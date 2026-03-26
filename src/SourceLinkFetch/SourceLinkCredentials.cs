using System.Net.Http.Headers;
using System.Text;

namespace SourceLinkFetch;

/// <summary>
/// Convenience methods for configuring an <see cref="HttpClient"/> with credentials
/// for accessing private repository source files through SourceLink URLs.
/// </summary>
/// <remarks>
/// Credentials are applied as HTTP request headers — never embedded in URLs.
/// Obtain token values from a secrets manager or secure configuration store;
/// do not hardcode them in source code.
/// </remarks>
public static class SourceLinkCredentials
{
    /// <summary>
    /// Configures the client to authenticate with GitHub (github.com or GitHub Enterprise)
    /// using a personal access token.
    /// </summary>
    /// <param name="client">The HTTP client used by <see cref="SourceLinkVerifier"/>.</param>
    /// <param name="token">
    /// A GitHub personal access token with <c>repo</c> scope (classic),
    /// or <c>contents:read</c> scope (fine-grained).
    /// </param>
    public static void ConfigureGitHub(HttpClient client, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Configures the client to authenticate with Azure DevOps
    /// (dev.azure.com, *.visualstudio.com, or an on-premises Azure DevOps Server)
    /// using a personal access token (PAT).
    /// </summary>
    /// <param name="client">The HTTP client used by <see cref="SourceLinkVerifier"/>.</param>
    /// <param name="personalAccessToken">
    /// An Azure DevOps personal access token with at least <c>Code (Read)</c> scope.
    /// </param>
    /// <seealso cref="ConfigureAzureDevOpsOAuth"/>
    public static void ConfigureAzureDevOps(HttpClient client, string personalAccessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personalAccessToken);
        // Azure DevOps Basic auth encodes an empty username and the PAT as password.
        string encoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($":{personalAccessToken}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", encoded);
    }

    /// <summary>
    /// Configures the client to authenticate with Azure DevOps using an
    /// AAD / Entra ID OAuth access token.
    /// </summary>
    /// <param name="client">The HTTP client used by <see cref="SourceLinkVerifier"/>.</param>
    /// <param name="aadToken">
    /// An Azure Active Directory (Entra ID) OAuth access token with the
    /// Azure DevOps <c>499b84ac-1321-427f-aa17-267ca6975798/.default</c> scope.
    /// </param>
    /// <seealso cref="ConfigureAzureDevOps"/>
    public static void ConfigureAzureDevOpsOAuth(HttpClient client, string aadToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aadToken);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", aadToken);
    }

    /// <summary>
    /// Configures the client to authenticate with GitLab (gitlab.com or a self-hosted instance)
    /// using a personal access token.
    /// </summary>
    /// <param name="client">The HTTP client used by <see cref="SourceLinkVerifier"/>.</param>
    /// <param name="token">
    /// A GitLab personal access token with <c>read_repository</c> scope.
    /// </param>
    public static void ConfigureGitLab(HttpClient client, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Configures the client to authenticate with Bitbucket Cloud (bitbucket.org)
    /// using an API token.
    /// </summary>
    /// <param name="client">The HTTP client used by <see cref="SourceLinkVerifier"/>.</param>
    /// <param name="token">
    /// A Bitbucket Cloud API token with repository read access.
    /// API tokens are the current standard; see <see cref="ConfigureBitbucketCloud"/> for
    /// the legacy app-password approach (deprecated, EOL June 2026).
    /// </param>
    public static void ConfigureBitbucketCloudToken(HttpClient client, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Configures the client to authenticate with Bitbucket Cloud (bitbucket.org)
    /// using a username and app password.
    /// </summary>
    /// <param name="client">The HTTP client used by <see cref="SourceLinkVerifier"/>.</param>
    /// <param name="username">Your Bitbucket username (not email address).</param>
    /// <param name="appPassword">
    /// A Bitbucket app password with <c>Repositories: Read</c> permission.
    /// </param>
    /// <remarks>
    /// App passwords are deprecated and will stop working June 2026.
    /// Use <see cref="ConfigureBitbucketCloudToken"/> with an API token instead.
    /// </remarks>
    [Obsolete("Bitbucket Cloud app passwords are deprecated and will stop working June 2026. " +
              "Use ConfigureBitbucketCloudToken with a Bitbucket API token instead.")]
    public static void ConfigureBitbucketCloud(HttpClient client, string username, string appPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(appPassword);
        string encoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{username}:{appPassword}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", encoded);
    }

    /// <summary>
    /// Configures the client to authenticate with Bitbucket Data Center / Server
    /// (self-hosted) using an HTTP access token.
    /// </summary>
    /// <param name="client">The HTTP client used by <see cref="SourceLinkVerifier"/>.</param>
    /// <param name="token">
    /// A Bitbucket HTTP access token with <c>Repository read</c> permission
    /// (requires Bitbucket Server 5.5 or later).
    /// </param>
    public static void ConfigureBitbucketServer(HttpClient client, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Configures the client to authenticate with a Gitea instance using an API token.
    /// </summary>
    /// <param name="client">The HTTP client used by <see cref="SourceLinkVerifier"/>.</param>
    /// <param name="token">
    /// A Gitea API token or fine-grained personal access token with repository read access.
    /// </param>
    public static void ConfigureGitea(HttpClient client, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Configures the client to use HTTP Basic authentication.
    /// Suitable for GitWeb or other self-hosted Git servers that require Basic auth.
    /// </summary>
    /// <param name="client">The HTTP client used by <see cref="SourceLinkVerifier"/>.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password or personal access token used as a password.</param>
    public static void ConfigureBasicAuth(HttpClient client, string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        string encoded = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", encoded);
    }
}
