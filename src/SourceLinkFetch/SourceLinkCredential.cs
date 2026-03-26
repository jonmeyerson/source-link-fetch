namespace SourceLinkFetch;

/// <summary>
/// An opaque credential passed to <see cref="ISourceLinkProvider.ConfigureAuth"/>.
/// Each provider knows which kind it requires and how to encode it as an HTTP header.
/// </summary>
/// <remarks>
/// Use <see cref="Token"/> for a single secret (PAT, API token).
/// Use <see cref="Basic"/> when the host requires a username and separate password
/// or app-password (e.g. Bitbucket Cloud).
/// </remarks>
public readonly struct SourceLinkCredential
{
    internal enum CredentialKind { Unset, Token, Basic }

    internal CredentialKind Kind { get; }
    internal string Primary { get; }    // token, or username
    internal string Secondary { get; }  // empty for Token, or password

    private SourceLinkCredential(CredentialKind kind, string primary, string secondary)
    {
        Kind = kind;
        Primary = primary;
        Secondary = secondary;
    }

    /// <summary>
    /// Creates a token-based credential (personal access token, API token, etc.).
    /// Used by GitHub, GitLab, Azure DevOps, Bitbucket Server, and Gitea.
    /// </summary>
    public static SourceLinkCredential Token(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return new(CredentialKind.Token, token, string.Empty);
    }

    /// <summary>
    /// Creates a username-and-password credential.
    /// Used by Bitbucket Cloud (username + app password) and GitWeb.
    /// </summary>
    public static SourceLinkCredential Basic(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return new(CredentialKind.Basic, username, password);
    }
}
