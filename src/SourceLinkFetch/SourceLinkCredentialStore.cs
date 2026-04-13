namespace SourceLinkFetch;

/// <summary>
/// Maps URL prefixes to credentials, injecting the correct
/// <c>Authorization</c> header per request — analogous to NuGet's
/// <c>packageSourceCredentials</c> in <c>NuGet.config</c>.
/// </summary>
/// <remarks>
/// <para>
/// Use this class when different repositories (or different organisations on
/// the same host) require different credentials.  A single
/// <see cref="System.Net.Http.HttpClient"/> built from
/// <see cref="CreateHandler"/> can be shared across all of them.
/// </para>
/// <para>
/// Credentials are applied per-request via an <see cref="System.Net.Http.DelegatingHandler"/>;
/// <see cref="System.Net.Http.HttpClient.DefaultRequestHeaders"/> is never modified.
/// </para>
/// </remarks>
public sealed class SourceLinkCredentialStore
{
    private readonly List<Entry> _entries = [];

    private sealed record Entry(string Prefix, ISourceLinkProvider Provider, SourceLinkCredential Credential);

    /// <summary>
    /// Registers a credential for every URL that starts with
    /// <paramref name="urlPrefix"/>. The provider is auto-detected via
    /// <see cref="SourceLinkProviders.Detect"/>.
    /// </summary>
    /// <param name="urlPrefix">
    /// The URL prefix to match, e.g.
    /// <c>https://raw.githubusercontent.com/my-org/</c>.
    /// If multiple registered prefixes match a request URL, the longest match wins.
    /// </param>
    /// <param name="credential">
    /// The credential to use for matching requests.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when no built-in provider recognises <paramref name="urlPrefix"/>.
    /// Pass the provider explicitly using
    /// <see cref="Add(string, ISourceLinkProvider, SourceLinkCredential)"/>
    /// for custom or enterprise hosts.
    /// </exception>
    public void Add(string urlPrefix, SourceLinkCredential credential)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(urlPrefix);

        var provider = SourceLinkProviders.Detect(urlPrefix)
            ?? throw new ArgumentException(
                $"No built-in provider recognised the URL prefix \"{urlPrefix}\". " +
                "Use the Add(urlPrefix, provider, credential) overload to supply a provider explicitly.",
                nameof(urlPrefix));

        Add(urlPrefix, provider, credential);
    }

    /// <summary>
    /// Registers a credential for every URL that starts with
    /// <paramref name="urlPrefix"/>, using the specified
    /// <paramref name="provider"/> to encode the credential.
    /// </summary>
    /// <param name="urlPrefix">
    /// The URL prefix to match, e.g.
    /// <c>https://github.mycompany.com/my-org/</c>.
    /// Longer prefixes take precedence over shorter ones.
    /// </param>
    /// <param name="provider">
    /// The provider whose <see cref="ISourceLinkProvider.GetAuthHeader"/> is
    /// called to build the <c>Authorization</c> header value.
    /// </param>
    /// <param name="credential">
    /// The credential to use for matching requests.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown immediately when <paramref name="credential"/> is not a valid kind
    /// for <paramref name="provider"/> (e.g. passing <see cref="SourceLinkCredential.Token"/>
    /// to a provider that only accepts <see cref="SourceLinkCredential.Bearer"/>).
    /// </exception>
    public void Add(string urlPrefix, ISourceLinkProvider provider, SourceLinkCredential credential)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(urlPrefix);
        ArgumentNullException.ThrowIfNull(provider);

        // Validate the credential kind against the provider eagerly so callers
        // get an ArgumentException at registration time, not during an HTTP request.
        try
        {
            provider.GetAuthHeader(credential);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException(
                $"The credential is not valid for provider \"{provider.Name}\": {ex.Message}",
                nameof(credential), ex);
        }

        _entries.Add(new Entry(urlPrefix, provider, credential));
    }

    /// <summary>
    /// Creates an <see cref="System.Net.Http.HttpMessageHandler"/> that intercepts every outgoing
    /// request and attaches the matching credential's <c>Authorization</c>
    /// header before forwarding the request.
    /// </summary>
    /// <remarks>
    /// Pass the returned handler to the <see cref="System.Net.Http.HttpClient"/> constructor:
    /// <code>
    /// var store = new SourceLinkCredentialStore();
    /// store.Add("https://raw.githubusercontent.com/my-org/", SourceLinkCredential.Token(pat));
    /// using var client = new HttpClient(store.CreateHandler());
    /// </code>
    /// </remarks>
    public HttpMessageHandler CreateHandler() =>
        new CredentialHandler(new List<Entry>(_entries));

    // -------------------------------------------------------------------------

    private sealed class CredentialHandler : DelegatingHandler
    {
        private readonly List<Entry> _entries;

        public CredentialHandler(List<Entry> entries)
            : base(new HttpClientHandler())
        {
            _entries = entries;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri is not null)
            {
                string url = request.RequestUri.AbsoluteUri;

                // Find the longest prefix that actually matches this URL.
                // Comparing across unrelated domains by length would be meaningless;
                // only prefixes that match the URL are candidates.
                Entry? best = null;
                foreach (var entry in _entries)
                {
                    if (url.StartsWith(entry.Prefix, StringComparison.OrdinalIgnoreCase) &&
                        (best is null || entry.Prefix.Length > best.Prefix.Length))
                    {
                        best = entry;
                    }
                }

                if (best is not null)
                    request.Headers.Authorization =
                        best.Provider.GetAuthHeader(best.Credential);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
