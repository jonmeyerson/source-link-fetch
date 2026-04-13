using System.Text.RegularExpressions;

namespace SourceLinkFetch.Tests;

public class SourceLinkReaderTests : IDisposable
{
    private static readonly SourceLinkReader Reader =
        SourceLinkReader.Open(typeof(SourceLinkReader).Assembly.Location);

    [Fact]
    public void HasEmbeddedPdb() =>
        Assert.True(Reader.HasPdb);

    [Fact]
    public void PdbIsEmbedded() =>
        Assert.Equal("Embedded", Reader.PdbLocation);

    [Fact]
    public void HasSourceLink() =>
        Assert.True(Reader.HasSourceLink);

    [Fact]
    public void SourceLinkJson_ContainsDocumentsProperty() =>
        Assert.Contains("documents", Reader.SourceLinkJson);

    [Fact]
    public void CommitHash_Is40HexChars() =>
        Assert.Matches(new Regex(@"^[0-9a-f]{40}$", RegexOptions.IgnoreCase), Reader.CommitHash);

    [Fact]
    public void RepositoryUrl_PointsToGitHub() =>
        Assert.Contains("github.com", Reader.RepositoryUrl);

    [Fact]
    public void EnumerateSourceDocuments_YieldsAtLeastOneCsFile()
    {
        var docs = Reader.EnumerateSourceDocuments().ToList();
        Assert.NotEmpty(docs);
        Assert.Contains(docs, d => d.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnumerateSourceDocuments_CsFiles_HaveResolvedUrls()
    {
        var docs = Reader.EnumerateSourceDocuments()
            .Where(d => d.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.All(docs, d => Assert.NotNull(d.ResolvedUrl));
    }

    [Fact]
    public void EnumerateSourceDocuments_ResolvedUrls_ContainCommitHash()
    {
        string? commitHash = Reader.CommitHash;
        Assert.NotNull(commitHash);

        var docs = Reader.EnumerateSourceDocuments()
            .Where(d => d.ResolvedUrl is not null)
            .ToList();

        Assert.All(docs, d => Assert.Contains(commitHash, d.ResolvedUrl));
    }

    public void Dispose() => Reader.Dispose();
}
