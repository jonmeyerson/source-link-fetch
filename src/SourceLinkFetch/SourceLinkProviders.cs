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

    // ---- Shared helpers (internal — used by provider implementations) ----

    // Most providers embed the commit hash as a 40-character hex path segment.
    internal static string? ExtractPathCommit(string urlTemplate)
    {
        var m = Regex.Match(urlTemplate, @"/([0-9a-f]{40})(?:/|$|\?)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    // Accepts Token OR Bearer — both produce Authorization: Bearer {token}.
    internal static AuthenticationHeaderValue BuildBearer(SourceLinkCredential cred, string name)
    {
        if (cred.Kind != SourceLinkCredential.CredentialKind.Token &&
            cred.Kind != SourceLinkCredential.CredentialKind.Bearer)
            throw new ArgumentException(
                $"{name} requires SourceLinkCredential.Token(token) or SourceLinkCredential.Bearer(token).",
                nameof(cred));
        return new AuthenticationHeaderValue("Bearer", cred.Primary);
    }

    internal static AuthenticationHeaderValue BuildBasic(SourceLinkCredential cred, string name)
    {
        if (cred.Kind != SourceLinkCredential.CredentialKind.Basic)
            throw new ArgumentException(
                $"{name} requires SourceLinkCredential.Basic(username, password).", nameof(cred));
        string encoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{cred.Primary}:{cred.Secondary}"));
        return new AuthenticationHeaderValue("Basic", encoded);
    }

    // Azure DevOps PAT uses Basic auth with an empty username: Basic :{pat}
    internal static AuthenticationHeaderValue BuildAzureDevOpsPat(SourceLinkCredential cred, string name)
    {
        if (cred.Kind != SourceLinkCredential.CredentialKind.Token)
            throw new ArgumentException(
                $"{name} requires SourceLinkCredential.Token(personalAccessToken).", nameof(cred));
        string encoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($":{cred.Primary}"));
        return new AuthenticationHeaderValue("Basic", encoded);
    }
}
