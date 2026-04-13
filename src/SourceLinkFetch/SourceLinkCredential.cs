namespace SourceLinkFetch;

/// <summary>
/// An opaque credential passed to <see cref="ISourceLinkProvider.ConfigureAuth"/>.
/// Each provider knows which kinds it accepts and how to encode them as HTTP headers.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><see cref="Token"/> — a personal access token (PAT). The provider
///     decides the encoding: most use <c>Authorization: Bearer</c>, but Azure
///     DevOps encodes PATs as <c>Authorization: Basic :{pat}</c>.</item>
///   <item><see cref="Bearer"/> — an OAuth or AAD/Entra access token. Always
///     sent as <c>Authorization: Bearer {token}</c> regardless of provider.</item>
///   <item><see cref="Basic"/> — a username and password (or app password).
///     Always sent as <c>Authorization: Basic {base64(user:password)}</c>.</item>
/// </list>
/// Credentials are applied as HTTP request headers — never embedded in URLs.
/// </remarks>
public readonly struct SourceLinkCredential
{
    internal enum CredentialKind { Unset, Token, Bearer, Basic }

    internal CredentialKind Kind { get; }
    internal string Primary { get; }    // token, or username
    internal string Secondary { get; }  // empty for Token/Bearer, or password

    private SourceLinkCredential(CredentialKind kind, string primary, string secondary)
    {
        Kind = kind;
        Primary = primary;
        Secondary = secondary;
    }

    /// <summary>
    /// A personal access token (PAT) or API key.
    /// The provider determines the wire encoding — use <see cref="Bearer"/> for
    /// OAuth / AAD tokens that must always be sent as <c>Authorization: Bearer</c>.
    /// </summary>
    public static SourceLinkCredential Token(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return new(CredentialKind.Token, token, string.Empty);
    }

    /// <summary>
    /// An OAuth, AAD, or Entra access token. Always sent as
    /// <c>Authorization: Bearer {token}</c> regardless of provider.
    /// Use <see cref="Token"/> for personal access tokens (PATs) — providers
    /// may encode those differently (e.g. Azure DevOps uses Basic auth for PATs).
    /// </summary>
    public static SourceLinkCredential Bearer(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return new(CredentialKind.Bearer, token, string.Empty);
    }

    /// <summary>
    /// A username and password or app password.
    /// Used by Bitbucket Cloud (username + app password) and GitWeb.
    /// </summary>
    public static SourceLinkCredential Basic(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return new(CredentialKind.Basic, username, password);
    }
}
