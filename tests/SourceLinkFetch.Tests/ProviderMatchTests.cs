namespace SourceLinkFetch.Tests;

public class ProviderMatchTests
{
    [Theory]
    [InlineData("https://raw.githubusercontent.com/org/repo/abc/src/Foo.cs", "GitHub")]
    [InlineData("https://gitlab.com/org/repo/-/raw/abc/src/Foo.cs", "GitLab")]
    [InlineData("https://gitlab.mycompany.com/org/repo/-/raw/abc/src/Foo.cs", "GitLab")]
    [InlineData("https://bitbucket.org/org/repo/raw/abc/src/Foo.cs", "Bitbucket Cloud")]
    [InlineData("https://bitbucket.mycompany.com/projects/P/repos/r/raw/src/Foo.cs", "Bitbucket Server")]
    [InlineData("https://gitea.mycompany.com/org/repo/raw/commit/abc/src/Foo.cs", "Gitea")]
    [InlineData("https://git.mycompany.com/gitweb?p=repo.git;a=blob_plain;f=Foo.cs", "GitWeb")]
    [InlineData("https://dev.azure.com/org/proj/_apis/git/repositories/repo/items", "Azure DevOps")]
    [InlineData("https://myaccount.visualstudio.com/proj/_apis/git/repositories/repo/items", "Azure DevOps")]
    public void Detect_KnownUrl_ReturnsCorrectProvider(string url, string expectedName) =>
        Assert.Equal(expectedName, SourceLinkProviders.Detect(url)?.Name);

    [Theory]
    [InlineData("https://example.com/some/path")]
    [InlineData("https://nuget.org/packages/Foo")]
    [InlineData(null)]
    public void Detect_UnknownUrl_ReturnsNull(string? url) =>
        Assert.Null(SourceLinkProviders.Detect(url));

    [Fact]
    public void All_ContainsSevenProviders() =>
        Assert.Equal(7, SourceLinkProviders.All.Count);

    [Fact]
    public void NamedProperties_AreSameInstancesAsAll()
    {
        Assert.Contains(SourceLinkProviders.GitHub, SourceLinkProviders.All);
        Assert.Contains(SourceLinkProviders.GitLab, SourceLinkProviders.All);
        Assert.Contains(SourceLinkProviders.BitbucketCloud, SourceLinkProviders.All);
        Assert.Contains(SourceLinkProviders.BitbucketServer, SourceLinkProviders.All);
        Assert.Contains(SourceLinkProviders.Gitea, SourceLinkProviders.All);
        Assert.Contains(SourceLinkProviders.GitWeb, SourceLinkProviders.All);
        Assert.Contains(SourceLinkProviders.AzureDevOps, SourceLinkProviders.All);
    }
}
