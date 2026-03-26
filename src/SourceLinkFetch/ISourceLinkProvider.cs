namespace SourceLinkFetch;

/// <summary>
/// Represents a source-control host that can produce SourceLink URLs.
/// </summary>
/// <remarks>
/// Built-in providers are exposed via <see cref="SourceLinkProviders"/>.
/// Implement this interface to support a custom or self-hosted host.
/// </remarks>
public interface ISourceLinkProvider
{
    /// <summary>Human-readable provider name (e.g. "GitHub").</summary>
    string Name { get; }

    /// <summary>
    /// Returns true when <paramref name="urlTemplate"/> was produced by this provider.
    /// The value may contain a <c>*</c> wildcard as stored in the SourceLink JSON.
    /// </summary>
    bool Matches(string urlTemplate);

    /// <summary>
    /// Extracts the repository root URL from a SourceLink URL template.
    /// Returns null when the template is not recognised.
    /// </summary>
    string? ExtractRepositoryUrl(string urlTemplate);

    /// <summary>
    /// Extracts the commit hash from a SourceLink URL template.
    /// Returns null when the commit cannot be determined.
    /// </summary>
    string? ExtractCommitHash(string urlTemplate);

    /// <summary>
    /// Converts a raw or API URL (as stored in the SourceLink JSON) to a URL
    /// that can be opened in a browser to view the file. Returns the original
    /// URL unchanged when no conversion is applicable.
    /// </summary>
    string? ToBrowseUrl(string resolvedUrl);

    /// <summary>
    /// Applies the appropriate <c>Authorization</c> header to <paramref name="client"/>
    /// so that requests to private repositories are authenticated.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> used by <see cref="SourceLinkVerifier"/>.</param>
    /// <param name="credential">
    /// A <see cref="SourceLinkCredential"/> of the kind expected by this provider.
    /// An <see cref="ArgumentException"/> is thrown when the wrong kind is supplied.
    /// </param>
    void ConfigureAuth(HttpClient client, SourceLinkCredential credential);
}
