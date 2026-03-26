namespace SourceLinkFetch.Tests;

public class UrlResolutionTests
{
    private const string Json = """
        {
          "documents": {
            "C:/src/MyApp/*": "https://raw.githubusercontent.com/org/repo/abc123/MyApp/*",
            "C:/src/Other/Exact.cs": "https://raw.githubusercontent.com/org/repo/abc123/Other/Exact.cs"
          }
        }
        """;

    private static readonly SourceLinkResolver Resolver =
        SourceLinkResolver.CreateFromJson(Json)!;

    [Fact]
    public void Wildcard_ResolvesFilePath() =>
        Assert.Equal(
            "https://raw.githubusercontent.com/org/repo/abc123/MyApp/Program.cs",
            Resolver.ResolveUrl("C:/src/MyApp/Program.cs"));

    [Fact]
    public void Wildcard_NormalisesBackslashes() =>
        Assert.Equal(
            "https://raw.githubusercontent.com/org/repo/abc123/MyApp/Program.cs",
            Resolver.ResolveUrl(@"C:\src\MyApp\Program.cs"));

    [Fact]
    public void Exact_Match_ReturnsUrl() =>
        Assert.Equal(
            "https://raw.githubusercontent.com/org/repo/abc123/Other/Exact.cs",
            Resolver.ResolveUrl("C:/src/Other/Exact.cs"));

    [Fact]
    public void NoMatch_ReturnsNull() =>
        Assert.Null(Resolver.ResolveUrl("C:/src/Unrelated/File.cs"));

    [Fact]
    public void CreateFromJson_EmptyDocuments_ReturnsNull()
    {
        const string empty = """{ "documents": {} }""";
        Assert.Null(SourceLinkResolver.CreateFromJson(empty));
    }

    [Fact]
    public void CreateFromJson_InvalidJson_ReturnsNull() =>
        Assert.Null(SourceLinkResolver.CreateFromJson("not json"));
}
