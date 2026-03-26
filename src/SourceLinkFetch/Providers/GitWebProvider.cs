using System.Net.Http.Headers;

namespace SourceLinkFetch;

internal sealed class GitWebProvider : ISourceLinkProvider
{
    public string Name => "GitWeb";

    public bool Matches(string url) =>
        url.Contains("/gitweb?", StringComparison.OrdinalIgnoreCase) &&
        url.Contains("a=blob_plain", StringComparison.OrdinalIgnoreCase);

    public string? ExtractRepositoryUrl(string urlTemplate)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            urlTemplate, @"https://([^/]+)/gitweb\?p=([^;]+\.git)");
        return m.Success
            ? $"https://{m.Groups[1].Value}/gitweb?p={m.Groups[2].Value}"
            : null;
    }

    public string? ExtractCommitHash(string urlTemplate)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            urlTemplate, @"[?;]hb=([0-9a-f]{40})(?:;|$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    public string? ToBrowseUrl(string url) =>
        SourceLinkResolver.ConvertToGitWebBrowseUrl(url);

    public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
        SourceLinkProviders.BuildBasic(credential, Name);
}
