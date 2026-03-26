namespace SourceLinkFetch.Tests;

public class ProviderExtractionTests
{
    private const string Sha = "abc123def456abc123def456abc123def456abc1";

    [Theory]
    [InlineData($"https://raw.githubusercontent.com/myorg/myrepo/{Sha}/src/Foo.cs",
                "https://github.com/myorg/myrepo")]
    public void GitHub_ExtractRepositoryUrl(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.GitHub.ExtractRepositoryUrl(template));

    [Theory]
    [InlineData($"https://raw.githubusercontent.com/org/repo/{Sha}/src/Foo.cs", Sha)]
    public void GitHub_ExtractCommitHash(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.GitHub.ExtractCommitHash(template));

    [Theory]
    [InlineData($"https://gitlab.com/myorg/myrepo/-/raw/{Sha}/src/Foo.cs",
                "https://gitlab.com/myorg/myrepo")]
    [InlineData($"https://gitlab.mycompany.com/myorg/myrepo/-/raw/{Sha}/src/Foo.cs",
                "https://gitlab.mycompany.com/myorg/myrepo")]
    public void GitLab_ExtractRepositoryUrl(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.GitLab.ExtractRepositoryUrl(template));

    [Theory]
    [InlineData($"https://gitlab.com/org/repo/-/raw/{Sha}/src/Foo.cs", Sha)]
    public void GitLab_ExtractCommitHash(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.GitLab.ExtractCommitHash(template));

    [Theory]
    [InlineData($"https://bitbucket.org/myorg/myrepo/raw/{Sha}/src/Foo.cs",
                "https://bitbucket.org/myorg/myrepo")]
    public void BitbucketCloud_ExtractRepositoryUrl(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.BitbucketCloud.ExtractRepositoryUrl(template));

    [Theory]
    [InlineData($"https://bitbucket.org/org/repo/raw/{Sha}/src/Foo.cs", Sha)]
    public void BitbucketCloud_ExtractCommitHash(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.BitbucketCloud.ExtractCommitHash(template));

    [Theory]
    [InlineData($"https://bitbucket.mycompany.com/projects/PROJ/repos/myrepo/raw/src/Foo.cs?at={Sha}",
                "https://bitbucket.mycompany.com/projects/PROJ/repos/myrepo")]
    public void BitbucketServer_ExtractRepositoryUrl(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.BitbucketServer.ExtractRepositoryUrl(template));

    [Theory]
    [InlineData($"https://bitbucket.mycompany.com/projects/P/repos/r/raw/src/Foo.cs?at={Sha}", Sha)]
    public void BitbucketServer_ExtractCommitHash(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.BitbucketServer.ExtractCommitHash(template));

    [Theory]
    [InlineData($"https://gitea.mycompany.com/myorg/myrepo/raw/commit/{Sha}/src/Foo.cs",
                "https://gitea.mycompany.com/myorg/myrepo")]
    public void Gitea_ExtractRepositoryUrl(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.Gitea.ExtractRepositoryUrl(template));

    [Theory]
    [InlineData($"https://gitea.mycompany.com/org/repo/raw/commit/{Sha}/src/Foo.cs", Sha)]
    public void Gitea_ExtractCommitHash(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.Gitea.ExtractCommitHash(template));

    [Theory]
    [InlineData($"https://git.mycompany.com/gitweb?p=myrepo.git;a=blob_plain;f=Foo.cs;hb={Sha}",
                "https://git.mycompany.com/gitweb?p=myrepo.git")]
    public void GitWeb_ExtractRepositoryUrl(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.GitWeb.ExtractRepositoryUrl(template));

    [Theory]
    [InlineData($"https://git.mycompany.com/gitweb?p=repo.git;a=blob_plain;f=Foo.cs;hb={Sha}", Sha)]
    public void GitWeb_ExtractCommitHash(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.GitWeb.ExtractCommitHash(template));

    [Theory]
    [InlineData($"https://dev.azure.com/myorg/myproject/_apis/git/repositories/myrepo/items?path=/src/Foo.cs&version={Sha}",
                "https://dev.azure.com/myorg/myproject/_git/myrepo")]
    [InlineData($"https://myaccount.visualstudio.com/myproject/_apis/git/repositories/myrepo/items?path=/src/Foo.cs&version={Sha}",
                "https://myaccount.visualstudio.com/myproject/_git/myrepo")]
    [InlineData($"https://ado.mycompany.com/DefaultCollection/myproject/_apis/git/repositories/myrepo/items?version={Sha}",
                "https://ado.mycompany.com/DefaultCollection/myproject/_git/myrepo")]
    public void AzureDevOps_ExtractRepositoryUrl(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.AzureDevOps.ExtractRepositoryUrl(template));

    [Theory]
    [InlineData($"https://dev.azure.com/org/proj/_apis/git/repositories/repo/items?version={Sha}", Sha)]
    public void AzureDevOps_ExtractCommitHash(string template, string expected) =>
        Assert.Equal(expected, SourceLinkProviders.AzureDevOps.ExtractCommitHash(template));
}
