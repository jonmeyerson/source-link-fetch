using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace SourceLinkFetch;

internal sealed class GiteaProvider : ISourceLinkProvider
{
    public string Name => "Gitea";

    public bool Matches(string url) =>
        url.Contains("/raw/commit/", StringComparison.OrdinalIgnoreCase);

    public string? ExtractRepositoryUrl(string urlTemplate)
    {
        var m = Regex.Match(urlTemplate,
            @"https://([^/]+)/([^/]+)/([^/]+)/raw/commit/");
        return m.Success
            ? $"https://{m.Groups[1].Value}/{m.Groups[2].Value}/{m.Groups[3].Value}"
            : null;
    }

    public string? ExtractCommitHash(string urlTemplate) =>
        SourceLinkProviders.ExtractPathCommit(urlTemplate);

    public string? ToBrowseUrl(string url) =>
        SourceLinkResolver.ConvertToGiteaBrowseUrl(url);

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
