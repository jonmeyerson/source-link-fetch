using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace SourceLinkFetch;

internal sealed class GitHubProvider : ISourceLinkProvider
{
    public string Name => "GitHub";

    public bool Matches(string url) =>
        url.Contains("//raw.githubusercontent.com/", StringComparison.OrdinalIgnoreCase);

    public string? ExtractRepositoryUrl(string urlTemplate)
    {
        var m = Regex.Match(urlTemplate,
            @"https://raw\.githubusercontent\.com/([^/]+)/([^/]+)/");
        return m.Success
            ? $"https://github.com/{m.Groups[1].Value}/{m.Groups[2].Value}"
            : null;
    }

    public string? ExtractCommitHash(string urlTemplate) =>
        SourceLinkProviders.ExtractPathCommit(urlTemplate);

    public string? ToBrowseUrl(string url) =>
        SourceLinkResolver.ConvertToGitHubBrowseUrl(url);

    public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
        SourceLinkProviders.BuildBearer(credential, Name);
}
