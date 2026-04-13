using System.Net;

namespace SourceLinkFetch;

/// <summary>
/// Describes the outcome of verifying a single SourceLink URL.
/// </summary>
public enum VerificationStatus
{
    /// <summary>The URL returned a 2xx response — source is accessible.</summary>
    Accessible,
    /// <summary>HTTP 401 — the URL exists but requires authentication. Retry with credentials.</summary>
    RequiresAuthentication,
    /// <summary>HTTP 403 — credentials were supplied but lack sufficient scope or permission.</summary>
    Forbidden,
    /// <summary>HTTP 404 — the URL does not exist (broken SourceLink or wrong commit).</summary>
    NotFound,
    /// <summary>HTTP 5xx or an unexpected non-success status — server-side problem.</summary>
    ServerError,
    /// <summary>No HTTP response was received (DNS failure, timeout, TLS error, etc.).</summary>
    NetworkError,
}

/// <summary>
/// Result of verifying a single source document's URL accessibility.
/// </summary>
public record VerificationResult
{
    /// <summary>The source file path as recorded in the PDB.</summary>
    public required string FilePath { get; init; }

    /// <summary>The resolved SourceLink URL that was checked.</summary>
    public string? Url { get; init; }

    /// <summary>The outcome of the HTTP check.</summary>
    public VerificationStatus Status { get; init; }

    /// <summary>The HTTP status code returned, or null for <see cref="VerificationStatus.NetworkError"/>.</summary>
    public int? HttpStatusCode { get; init; }

    /// <summary>Error message for network errors; null on any HTTP response.</summary>
    public string? Error { get; init; }

    /// <summary>True when <see cref="Status"/> is <see cref="VerificationStatus.Accessible"/>.</summary>
    public bool IsAccessible => Status == VerificationStatus.Accessible;
}

/// <summary>
/// Verifies that SourceLink URLs are accessible via HTTP HEAD requests.
/// </summary>
public static class SourceLinkVerifier
{
    /// <summary>
    /// Verifies all source documents by sending HTTP HEAD requests to their resolved URLs.
    /// </summary>
    /// <param name="documents">Source documents with resolved URLs.</param>
    /// <param name="client">HttpClient to use for requests.</param>
    /// <param name="maxConcurrency">Maximum parallel requests (default 16).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<IReadOnlyList<VerificationResult>> VerifyAsync(
        IEnumerable<SourceDocument> documents,
        HttpClient client,
        int maxConcurrency = 16,
        CancellationToken cancellationToken = default)
    {
        var docsWithUrls = documents.Where(d => d.ResolvedUrl is not null).ToList();

        if (docsWithUrls.Count == 0)
            return [];

        using var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = docsWithUrls.Select(doc => VerifyOneAsync(doc, client, semaphore, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results;
    }

    private static async Task<VerificationResult> VerifyOneAsync(
        SourceDocument doc, HttpClient client, SemaphoreSlim semaphore, CancellationToken ct)
    {
        await semaphore.WaitAsync(ct);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, doc.ResolvedUrl);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            var status = (int)response.StatusCode switch
            {
                401 => VerificationStatus.RequiresAuthentication,
                403 => VerificationStatus.Forbidden,
                404 => VerificationStatus.NotFound,
                >= 500 => VerificationStatus.ServerError,
                _ when response.IsSuccessStatusCode => VerificationStatus.Accessible,
                _ => VerificationStatus.ServerError,
            };

            return new VerificationResult
            {
                FilePath = doc.FilePath,
                Url = doc.ResolvedUrl,
                Status = status,
                HttpStatusCode = (int)response.StatusCode,
            };
        }
        catch (Exception ex)
        {
            return new VerificationResult
            {
                FilePath = doc.FilePath,
                Url = doc.ResolvedUrl,
                Status = VerificationStatus.NetworkError,
                Error = ex.Message,
            };
        }
        finally
        {
            semaphore.Release();
        }
    }
}
