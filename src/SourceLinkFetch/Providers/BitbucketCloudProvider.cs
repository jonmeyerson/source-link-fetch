using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace SourceLinkFetch;

internal sealed class BitbucketCloudProvider : ISourceLinkProvider
{
    public string Name => "Bitbucket Cloud";

    public bool Matches(string url) =>
        url.Contains("//bitbucket.org/", StringComparison.OrdinalIgnoreCase);

    public string? ExtractRepositoryUrl(string urlTemplate)
    {
        var m = Regex.Match(urlTemplate,
            @"https://bitbucket\.org/([^/]+)/([^/]+)/raw/");
        return m.Success
            ? $"https://bitbucket.org/{m.Groups[1].Value}/{m.Groups[2].Value}"
            : null;
    }

    public string? ExtractCommitHash(string urlTemplate) =>
        SourceLinkProviders.ExtractPathCommit(urlTemplate);

    public string? ToBrowseUrl(string url) =>
        SourceLinkResolver.ConvertToBitbucketCloudBrowseUrl(url);

    public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
        credential.Kind switch
        {
            SourceLinkCredential.CredentialKind.Bearer => SourceLinkProviders.BuildBearer(credential, Name),
            SourceLinkCredential.CredentialKind.Basic  => SourceLinkProviders.BuildBasic(credential, Name),
            _ => throw new ArgumentException(
                $"{Name} requires SourceLinkCredential.Bearer(apiToken) or " +
                $"SourceLinkCredential.Basic(username, appPassword). " +
                $"Note: Bitbucket Cloud app passwords are deprecated (EOL June 2026); " +
                $"prefer Bearer with an API token.",
                nameof(credential)),
        };
}
