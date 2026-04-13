using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace SourceLinkFetch;

internal sealed class BitbucketServerProvider : ISourceLinkProvider
{
    public string Name => "Bitbucket Server";

    public bool Matches(string url) =>
        url.Contains("/projects/", StringComparison.OrdinalIgnoreCase) &&
        url.Contains("/repos/", StringComparison.OrdinalIgnoreCase) &&
        url.Contains("/raw/", StringComparison.OrdinalIgnoreCase);

    public string? ExtractRepositoryUrl(string urlTemplate)
    {
        var m = Regex.Match(urlTemplate,
            @"https://([^/]+)/projects/([^/]+)/repos/([^/]+)/raw/");
        return m.Success
            ? $"https://{m.Groups[1].Value}/projects/{m.Groups[2].Value}/repos/{m.Groups[3].Value}"
            : null;
    }

    public string? ExtractCommitHash(string urlTemplate)
    {
        var m = Regex.Match(urlTemplate, @"[?&]at=([0-9a-f]{40})(?:&|$)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    public string? ToBrowseUrl(string url) =>
        SourceLinkResolver.ConvertToBitbucketServerBrowseUrl(url);

    public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
        credential.Kind switch
        {
            SourceLinkCredential.CredentialKind.Token  => SourceLinkProviders.BuildBearer(credential, Name),
            SourceLinkCredential.CredentialKind.Bearer => SourceLinkProviders.BuildBearer(credential, Name),
            SourceLinkCredential.CredentialKind.Basic  => SourceLinkProviders.BuildBasic(credential, Name),
            _ => throw new ArgumentException(
                $"{Name} requires Token, Bearer, or Basic credential.", nameof(credential)),
        };
}
