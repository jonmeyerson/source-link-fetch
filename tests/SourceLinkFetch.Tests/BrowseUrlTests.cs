namespace SourceLinkFetch.Tests;

public class BrowseUrlTests
{
    [Theory]
    [InlineData("https://raw.githubusercontent.com/org/repo/abc123def456abc123def456abc123def456abc1/src/Foo.cs",
                "https://github.com/org/repo/blob/abc123def456abc123def456abc123def456abc1/src/Foo.cs")]
    [InlineData("https://raw.githubusercontent.com/my-org/my-repo/deadbeefdeadbeefdeadbeefdeadbeefdeadbeef/README.md",
                "https://github.com/my-org/my-repo/blob/deadbeefdeadbeefdeadbeefdeadbeefdeadbeef/README.md")]
    public void GitHub_ConvertsToBlobUrl(string raw, string expected) =>
        Assert.Equal(expected, SourceLinkResolver.ConvertToGitHubBrowseUrl(raw));

    [Fact]
    public void GitHub_NullInput_ReturnsNull() =>
        Assert.Null(SourceLinkResolver.ConvertToGitHubBrowseUrl(null));

    [Fact]
    public void GitHub_NonGitHubUrl_ReturnsUnchanged()
    {
        const string url = "https://gitlab.com/org/repo/-/raw/abc/file.cs";
        Assert.Equal(url, SourceLinkResolver.ConvertToGitHubBrowseUrl(url));
    }

    [Theory]
    [InlineData("https://gitlab.com/org/repo/-/raw/abc123def456abc123def456abc123def456abc1/src/Foo.cs",
                "https://gitlab.com/org/repo/-/blob/abc123def456abc123def456abc123def456abc1/src/Foo.cs")]
    [InlineData("https://gitlab.mycompany.com/group/project/-/raw/main/README.md",
                "https://gitlab.mycompany.com/group/project/-/blob/main/README.md")]
    public void GitLab_ConvertsRawToBlob(string raw, string expected) =>
        Assert.Equal(expected, SourceLinkResolver.ConvertToGitLabBrowseUrl(raw));

    [Fact]
    public void GitLab_NullInput_ReturnsNull() =>
        Assert.Null(SourceLinkResolver.ConvertToGitLabBrowseUrl(null));

    [Theory]
    [InlineData("https://bitbucket.org/org/repo/raw/abc123def456abc123def456abc123def456abc1/src/Foo.cs",
                "https://bitbucket.org/org/repo/src/abc123def456abc123def456abc123def456abc1/src/Foo.cs")]
    public void BitbucketCloud_ConvertsRawToSrc(string raw, string expected) =>
        Assert.Equal(expected, SourceLinkResolver.ConvertToBitbucketCloudBrowseUrl(raw));

    [Fact]
    public void BitbucketCloud_NullInput_ReturnsNull() =>
        Assert.Null(SourceLinkResolver.ConvertToBitbucketCloudBrowseUrl(null));

    [Theory]
    [InlineData("https://bitbucket.mycompany.com/projects/PROJ/repos/myrepo/raw/src/Foo.cs?at=abc123def456abc123def456abc123def456abc1",
                "https://bitbucket.mycompany.com/projects/PROJ/repos/myrepo/browse/src/Foo.cs?at=abc123def456abc123def456abc123def456abc1")]
    public void BitbucketServer_ConvertsRawToBrowse(string raw, string expected) =>
        Assert.Equal(expected, SourceLinkResolver.ConvertToBitbucketServerBrowseUrl(raw));

    [Fact]
    public void BitbucketServer_NullInput_ReturnsNull() =>
        Assert.Null(SourceLinkResolver.ConvertToBitbucketServerBrowseUrl(null));

    [Theory]
    [InlineData("https://gitea.mycompany.com/org/repo/raw/commit/abc123def456abc123def456abc123def456abc1/src/Foo.cs",
                "https://gitea.mycompany.com/org/repo/src/commit/abc123def456abc123def456abc123def456abc1/src/Foo.cs")]
    public void Gitea_ConvertsRawCommitToSrcCommit(string raw, string expected) =>
        Assert.Equal(expected, SourceLinkResolver.ConvertToGiteaBrowseUrl(raw));

    [Fact]
    public void Gitea_NullInput_ReturnsNull() =>
        Assert.Null(SourceLinkResolver.ConvertToGiteaBrowseUrl(null));

    [Theory]
    [InlineData("https://git.mycompany.com/gitweb?p=myrepo.git;a=blob_plain;f=src/Foo.cs;hb=abc123def456abc123def456abc123def456abc1",
                "https://git.mycompany.com/gitweb?p=myrepo.git;a=blob;f=src/Foo.cs;hb=abc123def456abc123def456abc123def456abc1")]
    public void GitWeb_ConvertsBlobPlainToBlob(string raw, string expected) =>
        Assert.Equal(expected, SourceLinkResolver.ConvertToGitWebBrowseUrl(raw));

    [Fact]
    public void GitWeb_NullInput_ReturnsNull() =>
        Assert.Null(SourceLinkResolver.ConvertToGitWebBrowseUrl(null));

    [Theory]
    [InlineData(
        "https://dev.azure.com/myorg/myproject/_apis/git/repositories/myrepo/items?path=/src/Foo.cs&version=abc123def456abc123def456abc123def456abc1",
        "https://dev.azure.com/myorg/myproject/_git/myrepo?path=%2Fsrc%2FFoo.cs&version=GCabc123def456abc123def456abc123def456abc1")]
    [InlineData(
        "https://myaccount.visualstudio.com/myproject/_apis/git/repositories/myrepo/items?path=/src/Foo.cs&version=abc123def456abc123def456abc123def456abc1",
        "https://myaccount.visualstudio.com/myproject/_git/myrepo?path=%2Fsrc%2FFoo.cs&version=GCabc123def456abc123def456abc123def456abc1")]
    public void AzureDevOps_ConvertsApiUrlToBrowseUrl(string api, string expected) =>
        Assert.Equal(expected, SourceLinkResolver.ConvertToAzureDevOpsBrowseUrl(api));

    [Fact]
    public void AzureDevOps_NullInput_ReturnsNull() =>
        Assert.Null(SourceLinkResolver.ConvertToAzureDevOpsBrowseUrl(null));
}
