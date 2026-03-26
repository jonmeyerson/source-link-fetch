using System.Text;

namespace SourceLinkFetch.Tests;

public class ProviderAuthTests
{
    [Theory]
    [InlineData("GitHub")]
    [InlineData("GitLab")]
    public void Token_ProducesBearerHeader(string providerName)
    {
        var provider = SourceLinkProviders.All.Single(p => p.Name == providerName);
        var header = provider.GetAuthHeader(SourceLinkCredential.Token("mytoken"));
        Assert.Equal("Bearer", header.Scheme);
        Assert.Equal("mytoken", header.Parameter);
    }

    [Theory]
    [InlineData("GitHub")]
    [InlineData("GitLab")]
    public void Bearer_ProducesBearerHeader(string providerName)
    {
        var provider = SourceLinkProviders.All.Single(p => p.Name == providerName);
        var header = provider.GetAuthHeader(SourceLinkCredential.Bearer("oauthtoken"));
        Assert.Equal("Bearer", header.Scheme);
        Assert.Equal("oauthtoken", header.Parameter);
    }

    [Fact]
    public void AzureDevOps_Token_ProducesBasicPatHeader()
    {
        var header = SourceLinkProviders.AzureDevOps.GetAuthHeader(SourceLinkCredential.Token("mypat"));
        Assert.Equal("Basic", header.Scheme);
        string decoded = Encoding.ASCII.GetString(Convert.FromBase64String(header.Parameter!));
        Assert.Equal(":mypat", decoded);
    }

    [Fact]
    public void AzureDevOps_Bearer_ProducesBearerHeader()
    {
        var header = SourceLinkProviders.AzureDevOps.GetAuthHeader(SourceLinkCredential.Bearer("aadtoken"));
        Assert.Equal("Bearer", header.Scheme);
        Assert.Equal("aadtoken", header.Parameter);
    }

    [Fact]
    public void AzureDevOps_Basic_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SourceLinkProviders.AzureDevOps.GetAuthHeader(SourceLinkCredential.Basic("user", "pass")));

    [Fact]
    public void BitbucketCloud_Bearer_ProducesBearerHeader()
    {
        var header = SourceLinkProviders.BitbucketCloud.GetAuthHeader(SourceLinkCredential.Bearer("apitoken"));
        Assert.Equal("Bearer", header.Scheme);
        Assert.Equal("apitoken", header.Parameter);
    }

    [Fact]
    public void BitbucketCloud_Basic_ProducesBasicHeader()
    {
        var header = SourceLinkProviders.BitbucketCloud.GetAuthHeader(SourceLinkCredential.Basic("user", "apppw"));
        Assert.Equal("Basic", header.Scheme);
        string decoded = Encoding.ASCII.GetString(Convert.FromBase64String(header.Parameter!));
        Assert.Equal("user:apppw", decoded);
    }

    [Fact]
    public void BitbucketCloud_Token_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SourceLinkProviders.BitbucketCloud.GetAuthHeader(SourceLinkCredential.Token("token")));

    [Theory]
    [InlineData("Bitbucket Server")]
    [InlineData("Gitea")]
    public void Token_ProducesBearerHeader_ForMultiSchemeProviders(string providerName)
    {
        var provider = SourceLinkProviders.All.Single(p => p.Name == providerName);
        var header = provider.GetAuthHeader(SourceLinkCredential.Token("tok"));
        Assert.Equal("Bearer", header.Scheme);
    }

    [Theory]
    [InlineData("Bitbucket Server")]
    [InlineData("Gitea")]
    public void Basic_ProducesBasicHeader_ForMultiSchemeProviders(string providerName)
    {
        var provider = SourceLinkProviders.All.Single(p => p.Name == providerName);
        var header = provider.GetAuthHeader(SourceLinkCredential.Basic("user", "pass"));
        Assert.Equal("Basic", header.Scheme);
        string decoded = Encoding.ASCII.GetString(Convert.FromBase64String(header.Parameter!));
        Assert.Equal("user:pass", decoded);
    }

    [Fact]
    public void GitWeb_Basic_ProducesBasicHeader()
    {
        var header = SourceLinkProviders.GitWeb.GetAuthHeader(SourceLinkCredential.Basic("user", "pass"));
        Assert.Equal("Basic", header.Scheme);
    }

    [Fact]
    public void GitWeb_Token_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SourceLinkProviders.GitWeb.GetAuthHeader(SourceLinkCredential.Token("token")));
}
