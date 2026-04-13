using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace SourceLinkFetch;

internal sealed class AzureDevOpsProvider : ISourceLinkProvider
{
    public string Name => "Azure DevOps";

    public bool Matches(string url) =>
        url.Contains("//dev.azure.com/", StringComparison.OrdinalIgnoreCase) ||
        url.Contains(".visualstudio.com/", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("/_apis/git/repositories/", StringComparison.OrdinalIgnoreCase);

    public string? ExtractRepositoryUrl(string urlTemplate)
    {
        var m = Regex.Match(urlTemplate,
            @"https://dev\.azure\.com/([^/]+)/([^/]+)/_apis/git/repositories/([^/?]+)");
        if (m.Success)
            return $"https://dev.azure.com/{m.Groups[1].Value}/{m.Groups[2].Value}/_git/{m.Groups[3].Value}";

        m = Regex.Match(urlTemplate,
            @"https://([^.]+)\.visualstudio\.com/([^/]+)/_apis/git/repositories/([^/?]+)");
        if (m.Success)
            return $"https://{m.Groups[1].Value}.visualstudio.com/{m.Groups[2].Value}/_git/{m.Groups[3].Value}";

        m = Regex.Match(urlTemplate,
            @"https://([^/]+)/([^/]+)/([^/]+)/_apis/git/repositories/([^/?]+)");
        if (m.Success)
            return $"https://{m.Groups[1].Value}/{m.Groups[2].Value}/{m.Groups[3].Value}/_git/{m.Groups[4].Value}";

        return null;
    }

    public string? ExtractCommitHash(string urlTemplate)
    {
        var m = Regex.Match(urlTemplate, @"[?&]version=([0-9a-f]{40})(?:&|$)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    public string? ToBrowseUrl(string url) =>
        SourceLinkResolver.ConvertToAzureDevOpsBrowseUrl(url);

    public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
        credential.Kind switch
        {
            SourceLinkCredential.CredentialKind.Token  => SourceLinkProviders.BuildAzureDevOpsPat(credential, Name),
            SourceLinkCredential.CredentialKind.Bearer => SourceLinkProviders.BuildBearer(credential, Name),
            _ => throw new ArgumentException(
                $"{Name} requires SourceLinkCredential.Token(personalAccessToken) for PATs " +
                $"or SourceLinkCredential.Bearer(token) for AAD/Entra OAuth tokens.",
                nameof(credential)),
        };
}
