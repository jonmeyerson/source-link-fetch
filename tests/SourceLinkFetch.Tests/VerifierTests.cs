using System.Net;

namespace SourceLinkFetch.Tests;

public class VerifierTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK,           VerificationStatus.Accessible)]
    [InlineData(HttpStatusCode.NoContent,    VerificationStatus.Accessible)]
    [InlineData(HttpStatusCode.Unauthorized, VerificationStatus.RequiresAuthentication)]
    [InlineData(HttpStatusCode.Forbidden,    VerificationStatus.Forbidden)]
    [InlineData(HttpStatusCode.NotFound,     VerificationStatus.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError, VerificationStatus.ServerError)]
    [InlineData(HttpStatusCode.BadGateway,   VerificationStatus.ServerError)]
    public async Task StatusCode_MapsToExpectedVerificationStatus(HttpStatusCode code, VerificationStatus expected)
    {
        using var client = FakeClient(code);
        var doc = new SourceDocument("/src/Foo.cs", false, "https://example.com/Foo.cs");

        var result = await SourceLinkVerifier.VerifyAsync([doc], client);

        var single = Assert.Single(result);
        Assert.Equal(expected, single.Status);
        Assert.Equal("/src/Foo.cs", single.FilePath);
        Assert.Equal("https://example.com/Foo.cs", single.Url);
    }

    [Fact]
    public async Task Accessible_IsAccessibleProperty_IsTrue()
    {
        using var client = FakeClient(HttpStatusCode.OK);
        var doc = new SourceDocument("/src/Foo.cs", false, "https://example.com/Foo.cs");

        var result = await SourceLinkVerifier.VerifyAsync([doc], client);

        Assert.True(Assert.Single(result).IsAccessible);
    }

    [Fact]
    public async Task NonAccessibleStatus_IsAccessibleProperty_IsFalse()
    {
        using var client = FakeClient(HttpStatusCode.NotFound);
        var doc = new SourceDocument("/src/Foo.cs", false, "https://example.com/Foo.cs");

        var result = await SourceLinkVerifier.VerifyAsync([doc], client);

        Assert.False(Assert.Single(result).IsAccessible);
    }

    [Fact]
    public async Task NetworkException_MapsToNetworkError()
    {
        using var client = ThrowingClient();
        var doc = new SourceDocument("/src/Foo.cs", false, "https://example.com/Foo.cs");

        var result = await SourceLinkVerifier.VerifyAsync([doc], client);

        var single = Assert.Single(result);
        Assert.Equal(VerificationStatus.NetworkError, single.Status);
        Assert.NotNull(single.Error);
        Assert.Null(single.HttpStatusCode);
    }

    [Fact]
    public async Task NullUrl_SkipsVerification_NotIncludedInResults()
    {
        using var client = FakeClient(HttpStatusCode.OK);
        var doc = new SourceDocument("/src/Foo.cs", false, null);

        var result = await SourceLinkVerifier.VerifyAsync([doc], client);

        Assert.Empty(result);
    }

    [Fact]
    public async Task MultipleDocuments_AllVerified()
    {
        using var client = FakeClient(HttpStatusCode.OK);
        var docs = new[]
        {
            new SourceDocument("/src/A.cs", false, "https://example.com/A.cs"),
            new SourceDocument("/src/B.cs", false, "https://example.com/B.cs"),
            new SourceDocument("/src/C.cs", false, "https://example.com/C.cs"),
        };

        var result = await SourceLinkVerifier.VerifyAsync(docs, client);

        Assert.Equal(3, result.Count);
        Assert.All(result, r => Assert.Equal(VerificationStatus.Accessible, r.Status));
    }

    // ---- helpers ----

    private static HttpClient FakeClient(HttpStatusCode statusCode) =>
        new(new FakeHandler(statusCode));

    private static HttpClient ThrowingClient() =>
        new(new ThrowingHandler());

    private sealed class FakeHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("Simulated network failure");
    }
}
