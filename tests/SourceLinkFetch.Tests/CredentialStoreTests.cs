using System.Net;
using System.Net.Http.Headers;

namespace SourceLinkFetch.Tests;

public class CredentialStoreTests
{
    [Fact]
    public async Task SingleEntry_MatchingUrl_SetsAuthorizationHeader()
    {
        var store = new SourceLinkCredentialStore();
        store.Add("https://raw.githubusercontent.com/org/", SourceLinkCredential.Token("mytoken"));

        var request = await SendThroughStoreAsync(store, "https://raw.githubusercontent.com/org/repo/abc/src/Foo.cs");

        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("mytoken", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SingleEntry_NonMatchingUrl_LeavesHeaderAbsent()
    {
        var store = new SourceLinkCredentialStore();
        store.Add("https://raw.githubusercontent.com/org/", SourceLinkCredential.Token("mytoken"));

        var request = await SendThroughStoreAsync(store, "https://dev.azure.com/other/proj/_apis/git/repositories/repo/items");

        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task LongestPrefix_WinsOverShorterPrefix()
    {
        var store = new SourceLinkCredentialStore();
        store.Add("https://raw.githubusercontent.com/org/", SourceLinkCredential.Token("org-token"));
        store.Add("https://raw.githubusercontent.com/org/specific-repo/", SourceLinkCredential.Token("repo-token"));

        var specificRequest = await SendThroughStoreAsync(store,
            "https://raw.githubusercontent.com/org/specific-repo/abc/src/Foo.cs");
        Assert.Equal("repo-token", specificRequest.Headers.Authorization?.Parameter);

        var orgRequest = await SendThroughStoreAsync(store,
            "https://raw.githubusercontent.com/org/other-repo/abc/src/Foo.cs");
        Assert.Equal("org-token", orgRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task UnrelatedDomains_DoNotInterfere()
    {
        var store = new SourceLinkCredentialStore();
        store.Add("https://raw.githubusercontent.com/org/", SourceLinkCredential.Token("github-token"));
        store.Add("https://dev.azure.com/company/", SourceLinkCredential.Token("ado-pat"));

        var githubRequest = await SendThroughStoreAsync(store,
            "https://raw.githubusercontent.com/org/repo/abc/src/Foo.cs");
        Assert.Equal("Bearer", githubRequest.Headers.Authorization?.Scheme);
        Assert.Equal("github-token", githubRequest.Headers.Authorization?.Parameter);

        var adoRequest = await SendThroughStoreAsync(store,
            "https://dev.azure.com/company/proj/_apis/git/repositories/repo/items?version=abc");
        Assert.Equal("Basic", adoRequest.Headers.Authorization?.Scheme);
    }

    [Fact]
    public void Add_WrongCredentialKind_ThrowsAtRegistrationTime()
    {
        var store = new SourceLinkCredentialStore();
        Assert.Throws<ArgumentException>(() =>
            store.Add("https://raw.githubusercontent.com/org/",
                SourceLinkProviders.GitHub,
                SourceLinkCredential.Basic("user", "pass")));
    }

    [Fact]
    public void Add_UnknownPrefix_ThrowsArgumentException()
    {
        var store = new SourceLinkCredentialStore();
        Assert.Throws<ArgumentException>(() =>
            store.Add("https://unknown-host.example.com/", SourceLinkCredential.Token("tok")));
    }

    [Fact]
    public async Task CreateHandler_IsSnapshot_LaterAddDoesNotAffectExistingHandler()
    {
        var store = new SourceLinkCredentialStore();
        store.Add("https://raw.githubusercontent.com/org/", SourceLinkCredential.Token("original"));

        var handler = BuildInvocableHandler(store);

        store.Add("https://raw.githubusercontent.com/org/", SourceLinkCredential.Token("added-later"));

        var request = new HttpRequestMessage(HttpMethod.Head,
            "https://raw.githubusercontent.com/org/repo/abc/src/Foo.cs");
        await handler.SendAsync(request, default);

        Assert.Equal("original", request.Headers.Authorization?.Parameter);
    }

    // ---- helpers ----

    private static async Task<HttpRequestMessage> SendThroughStoreAsync(SourceLinkCredentialStore store, string url)
    {
        var invoker = BuildInvocableHandler(store);
        var request = new HttpRequestMessage(HttpMethod.Head, url);
        await invoker.SendAsync(request, default);
        return request;
    }

    private static HttpMessageInvoker BuildInvocableHandler(SourceLinkCredentialStore store)
    {
        var credHandler = (DelegatingHandler)store.CreateHandler();
        credHandler.InnerHandler = new FakeHandler(HttpStatusCode.OK);
        return new HttpMessageInvoker(credHandler);
    }

    private sealed class FakeHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
