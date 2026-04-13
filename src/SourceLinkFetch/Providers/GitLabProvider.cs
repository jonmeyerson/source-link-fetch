using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace SourceLinkFetch;

internal sealed class GitLabProvider : ISourceLinkProvider
{
    public string Name => "GitLab";

    public bool Matches(string url) =>
        url.Contains("/-/raw/", StringComparison.OrdinalIgnoreCase);

    public string? ExtractRepositoryUrl(string urlTemplate)
    {
        var m = Regex.Match(urlTemplate,
            @"https://([^/]+)/([^/]+)/([^/]+)/-/raw/");
        return m.Success
            ? $"https://{m.Groups[1].Value}/{m.Groups[2].Value}/{m.Groups[3].Value}"
            : null;
    }

    public string? ExtractCommitHash(string urlTemplate) =>
        SourceLinkProviders.ExtractPathCommit(urlTemplate);

    public string? ToBrowseUrl(string url) =>
        SourceLinkResolver.ConvertToGitLabBrowseUrl(url);

    public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
        SourceLinkProviders.BuildBearer(credential, Name);
}
