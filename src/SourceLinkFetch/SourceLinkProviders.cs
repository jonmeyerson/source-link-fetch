using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace SourceLinkFetch;

/// <summary>
/// Registry of built-in <see cref="ISourceLinkProvider"/> implementations and a
/// factory for detecting which provider produced a given SourceLink URL.
/// </summary>
public static class SourceLinkProviders
{
    /// <summary>GitHub (github.com or GitHub Enterprise).</summary>
    public static ISourceLinkProvider GitHub { get; } = new GitHubProvider();

    /// <summary>GitLab (gitlab.com or self-hosted).</summary>
    public static ISourceLinkProvider GitLab { get; } = new GitLabProvider();

    /// <summary>Bitbucket Cloud (bitbucket.org).</summary>
    public static ISourceLinkProvider BitbucketCloud { get; } = new BitbucketCloudProvider();

    /// <summary>Bitbucket Server / Data Center (self-hosted).</summary>
    public static ISourceLinkProvider BitbucketServer { get; } = new BitbucketServerProvider();

    /// <summary>Gitea (self-hosted).</summary>
    public static ISourceLinkProvider Gitea { get; } = new GiteaProvider();

    /// <summary>GitWeb (self-hosted).</summary>
    public static ISourceLinkProvider GitWeb { get; } = new GitWebProvider();

    /// <summary>Azure DevOps (dev.azure.com, *.visualstudio.com, or on-premises).</summary>
    public static ISourceLinkProvider AzureDevOps { get; } = new AzureDevOpsProvider();

    /// <summary>
    /// All built-in providers in detection-priority order.
    /// Add custom providers before passing to <see cref="Detect"/> if needed.
    /// </summary>
    public static IReadOnlyList<ISourceLinkProvider> All { get; } =
    [
        GitHub, GitLab, BitbucketCloud, BitbucketServer, Gitea, GitWeb, AzureDevOps,
    ];

    /// <summary>
    /// Returns the first provider in <see cref="All"/> whose <see cref="ISourceLinkProvider.Matches"/>
    /// returns true for <paramref name="urlTemplate"/>, or null if none match.
    /// </summary>
    public static ISourceLinkProvider? Detect(string? urlTemplate)
    {
        if (urlTemplate is null) return null;
        foreach (var provider in All)
            if (provider.Matches(urlTemplate))
                return provider;
        return null;
    }

    // ---- Shared helpers ---------------------------------------------

    // Most providers embed the commit hash as a 40-character hex path segment.
    private static string? ExtractPathCommit(string urlTemplate)
    {
        var m = Regex.Match(urlTemplate, @"/([0-9a-f]{40})(?:/|$|\?)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    // ---- Auth header builders ---------------------------------------
    // Each returns an AuthenticationHeaderValue rather than setting DefaultRequestHeaders,
    // so the value can be applied per-request by SourceLinkCredentialStore.

    // Accepts Token OR Bearer — both produce Authorization: Bearer {token}.
    private static AuthenticationHeaderValue BuildBearer(SourceLinkCredential cred, string name)
    {
        if (cred.Kind != SourceLinkCredential.CredentialKind.Token &&
            cred.Kind != SourceLinkCredential.CredentialKind.Bearer)
            throw new ArgumentException(
                $"{name} requires SourceLinkCredential.Token(token) or SourceLinkCredential.Bearer(token).",
                nameof(cred));
        return new AuthenticationHeaderValue("Bearer", cred.Primary);
    }

    private static AuthenticationHeaderValue BuildBasic(SourceLinkCredential cred, string name)
    {
        if (cred.Kind != SourceLinkCredential.CredentialKind.Basic)
            throw new ArgumentException(
                $"{name} requires SourceLinkCredential.Basic(username, password).", nameof(cred));
        string encoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{cred.Primary}:{cred.Secondary}"));
        return new AuthenticationHeaderValue("Basic", encoded);
    }

    // Azure DevOps PAT uses Basic auth with an empty username: Basic :{pat}
    private static AuthenticationHeaderValue BuildAzureDevOpsPat(SourceLinkCredential cred, string name)
    {
        if (cred.Kind != SourceLinkCredential.CredentialKind.Token)
            throw new ArgumentException(
                $"{name} requires SourceLinkCredential.Token(personalAccessToken).", nameof(cred));
        string encoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($":{cred.Primary}"));
        return new AuthenticationHeaderValue("Basic", encoded);
    }

    // ---- Provider implementations -----------------------------------

    private sealed class GitHubProvider : ISourceLinkProvider
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

        public string? ExtractCommitHash(string urlTemplate) => ExtractPathCommit(urlTemplate);

        public string? ToBrowseUrl(string url) =>
            SourceLinkResolver.ConvertToGitHubBrowseUrl(url);

        // Accepts Token (PAT) or Bearer (OAuth) — both become Authorization: Bearer.
        public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
            BuildBearer(credential, Name);
    }

    private sealed class GitLabProvider : ISourceLinkProvider
    {
        public string Name => "GitLab";

        // /-/ is a GitLab-specific path separator that distinguishes it from all other hosts.
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

        public string? ExtractCommitHash(string urlTemplate) => ExtractPathCommit(urlTemplate);

        public string? ToBrowseUrl(string url) =>
            SourceLinkResolver.ConvertToGitLabBrowseUrl(url);

        // Accepts Token (PAT) or Bearer (OAuth) — both become Authorization: Bearer.
        public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
            BuildBearer(credential, Name);
    }

    private sealed class BitbucketCloudProvider : ISourceLinkProvider
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

        public string? ExtractCommitHash(string urlTemplate) => ExtractPathCommit(urlTemplate);

        public string? ToBrowseUrl(string url) =>
            SourceLinkResolver.ConvertToBitbucketCloudBrowseUrl(url);

        // Accepts Bearer (API token / OAuth — the current standard) or
        // Basic (app password — deprecated, EOL June 2026).
        // Token is intentionally rejected: use Bearer for Bitbucket API tokens.
        public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
            credential.Kind switch
            {
                SourceLinkCredential.CredentialKind.Bearer => BuildBearer(credential, Name),
                SourceLinkCredential.CredentialKind.Basic  => BuildBasic(credential, Name),
                _ => throw new ArgumentException(
                    $"{Name} requires SourceLinkCredential.Bearer(apiToken) or " +
                    $"SourceLinkCredential.Basic(username, appPassword). " +
                    $"Note: Bitbucket Cloud app passwords are deprecated (EOL June 2026); " +
                    $"prefer Bearer with an API token.",
                    nameof(credential)),
            };
    }

    private sealed class BitbucketServerProvider : ISourceLinkProvider
    {
        public string Name => "Bitbucket Server";

        // /projects/.../repos/.../raw/ is the Bitbucket Server raw file path structure.
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

        // Bitbucket Server stores the commit in the ?at= query parameter.
        public string? ExtractCommitHash(string urlTemplate)
        {
            var m = Regex.Match(urlTemplate, @"[?&]at=([0-9a-f]{40})(?:&|$)", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        public string? ToBrowseUrl(string url) =>
            SourceLinkResolver.ConvertToBitbucketServerBrowseUrl(url);

        // Accepts Token or Bearer (project/repo HTTP access tokens → Bearer header)
        // or Basic (user-level tokens as password, or username:password).
        public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
            credential.Kind switch
            {
                SourceLinkCredential.CredentialKind.Token  => BuildBearer(credential, Name),
                SourceLinkCredential.CredentialKind.Bearer => BuildBearer(credential, Name),
                SourceLinkCredential.CredentialKind.Basic  => BuildBasic(credential, Name),
                _ => throw new ArgumentException(
                    $"{Name} requires Token, Bearer, or Basic credential.", nameof(credential)),
            };
    }

    private sealed class GiteaProvider : ISourceLinkProvider
    {
        public string Name => "Gitea";

        // /raw/commit/ is Gitea's raw file URL pattern (distinct from Bitbucket's /raw/{sha}/).
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

        public string? ExtractCommitHash(string urlTemplate) => ExtractPathCommit(urlTemplate);

        public string? ToBrowseUrl(string url) =>
            SourceLinkResolver.ConvertToGiteaBrowseUrl(url);

        // Accepts Token or Bearer (API tokens → Bearer header) or Basic (username:password).
        public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
            credential.Kind switch
            {
                SourceLinkCredential.CredentialKind.Token  => BuildBearer(credential, Name),
                SourceLinkCredential.CredentialKind.Bearer => BuildBearer(credential, Name),
                SourceLinkCredential.CredentialKind.Basic  => BuildBasic(credential, Name),
                _ => throw new ArgumentException(
                    $"{Name} requires Token, Bearer, or Basic credential.", nameof(credential)),
            };
    }

    private sealed class GitWebProvider : ISourceLinkProvider
    {
        public string Name => "GitWeb";

        public bool Matches(string url) =>
            url.Contains("/gitweb?", StringComparison.OrdinalIgnoreCase) &&
            url.Contains("a=blob_plain", StringComparison.OrdinalIgnoreCase);

        public string? ExtractRepositoryUrl(string urlTemplate)
        {
            var m = Regex.Match(urlTemplate, @"https://([^/]+)/gitweb\?p=([^;]+\.git)");
            return m.Success
                ? $"https://{m.Groups[1].Value}/gitweb?p={m.Groups[2].Value}"
                : null;
        }

        // GitWeb stores the commit in the ;hb= semicolon-delimited query parameter.
        public string? ExtractCommitHash(string urlTemplate)
        {
            var m = Regex.Match(urlTemplate, @"[?;]hb=([0-9a-f]{40})(?:;|$)", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        public string? ToBrowseUrl(string url) =>
            SourceLinkResolver.ConvertToGitWebBrowseUrl(url);

        // GitWeb is protected at the HTTP server layer; only Basic auth is applicable.
        public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
            BuildBasic(credential, Name);
    }

    private sealed class AzureDevOpsProvider : ISourceLinkProvider
    {
        public string Name => "Azure DevOps";

        // Matches cloud (dev.azure.com), legacy (*.visualstudio.com), and on-premises
        // (any host with the /_apis/git/repositories/ path).
        public bool Matches(string url) =>
            url.Contains("//dev.azure.com/", StringComparison.OrdinalIgnoreCase) ||
            url.Contains(".visualstudio.com/", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("/_apis/git/repositories/", StringComparison.OrdinalIgnoreCase);

        public string? ExtractRepositoryUrl(string urlTemplate)
        {
            // Cloud: dev.azure.com/{org}/{project}/_apis/git/repositories/{repo}
            var m = Regex.Match(urlTemplate,
                @"https://dev\.azure\.com/([^/]+)/([^/]+)/_apis/git/repositories/([^/?]+)");
            if (m.Success)
                return $"https://dev.azure.com/{m.Groups[1].Value}/{m.Groups[2].Value}/_git/{m.Groups[3].Value}";

            // Legacy: {account}.visualstudio.com/{project}/_apis/git/repositories/{repo}
            m = Regex.Match(urlTemplate,
                @"https://([^.]+)\.visualstudio\.com/([^/]+)/_apis/git/repositories/([^/?]+)");
            if (m.Success)
                return $"https://{m.Groups[1].Value}.visualstudio.com/{m.Groups[2].Value}/_git/{m.Groups[3].Value}";

            // On-premises: {host}/{collection}/{project}/_apis/git/repositories/{repo}
            m = Regex.Match(urlTemplate,
                @"https://([^/]+)/([^/]+)/([^/]+)/_apis/git/repositories/([^/?]+)");
            if (m.Success)
                return $"https://{m.Groups[1].Value}/{m.Groups[2].Value}/{m.Groups[3].Value}/_git/{m.Groups[4].Value}";

            return null;
        }

        // Azure DevOps stores the commit in the ?version= query parameter.
        public string? ExtractCommitHash(string urlTemplate)
        {
            var m = Regex.Match(urlTemplate, @"[?&]version=([0-9a-f]{40})(?:&|$)", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        public string? ToBrowseUrl(string url) =>
            SourceLinkResolver.ConvertToAzureDevOpsBrowseUrl(url);

        // Accepts Token (PAT → Basic :{pat}) or Bearer (AAD/Entra OAuth → Bearer {token}).
        public AuthenticationHeaderValue GetAuthHeader(SourceLinkCredential credential) =>
            credential.Kind switch
            {
                SourceLinkCredential.CredentialKind.Token  => BuildAzureDevOpsPat(credential, Name),
                SourceLinkCredential.CredentialKind.Bearer => BuildBearer(credential, Name),
                _ => throw new ArgumentException(
                    $"{Name} requires SourceLinkCredential.Token(personalAccessToken) for PATs " +
                    $"or SourceLinkCredential.Bearer(token) for AAD/Entra OAuth tokens.",
                    nameof(credential)),
            };
    }
}
