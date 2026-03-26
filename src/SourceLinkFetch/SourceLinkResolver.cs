using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SourceLinkFetch;

/// <summary>
/// Parses SourceLink JSON and maps file paths to source URLs using glob patterns.
/// </summary>
public class SourceLinkResolver
{
    // SourceLink GUID: CC110556-A091-4D38-9FEC-25AB9A351A6A
    private static readonly Guid SourceLinkGuid = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");

    private readonly Dictionary<string, string> _documentMappings;

    private SourceLinkResolver(Dictionary<string, string> documentMappings)
    {
        _documentMappings = documentMappings;
    }

    /// <summary>
    /// Creates a SourceLinkResolver from a PDB metadata reader.
    /// Returns null if no SourceLink information is available.
    /// </summary>
    public static SourceLinkResolver? Create(MetadataReader pdbReader)
    {
        string? sourceLinkJson = ExtractSourceLinkJson(pdbReader);
        if (sourceLinkJson == null)
            return null;

        var mappings = ParseMappings(sourceLinkJson);
        if (mappings.Count == 0)
            return null;

        return new SourceLinkResolver(mappings);
    }

    /// <summary>
    /// Applies SourceLink URL pattern to convert a file path to a source URL.
    /// </summary>
    public string? ResolveUrl(string filePath)
    {
        filePath = filePath.Replace('\\', '/');

        foreach (var (pattern, urlTemplate) in _documentMappings)
        {
            if (pattern.Contains('*'))
            {
                string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", "(.*)") + "$";
                var match = Regex.Match(filePath, regexPattern);

                if (match.Success && match.Groups.Count > 1)
                {
                    string captured = match.Groups[1].Value;
                    return urlTemplate.Replace("*", captured);
                }
            }
            else if (filePath == pattern)
            {
                return urlTemplate;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the repository URL from SourceLink document mappings.
    /// Supports GitHub, Azure DevOps (dev.azure.com), and Azure DevOps legacy (*.visualstudio.com).
    /// </summary>
    public string? ExtractRepositoryUrl()
    {
        foreach (var (_, urlTemplate) in _documentMappings)
        {
            // GitHub: https://raw.githubusercontent.com/{owner}/{repo}/
            var githubMatch = Regex.Match(urlTemplate,
                @"https://raw\.githubusercontent\.com/([^/]+)/([^/]+)/");
            if (githubMatch.Success)
                return $"https://github.com/{githubMatch.Groups[1].Value}/{githubMatch.Groups[2].Value}";

            // Azure DevOps: https://dev.azure.com/{org}/{project}/_apis/git/repositories/{repo}/items
            var adoMatch = Regex.Match(urlTemplate,
                @"https://dev\.azure\.com/([^/]+)/([^/]+)/_apis/git/repositories/([^/?]+)");
            if (adoMatch.Success)
                return $"https://dev.azure.com/{adoMatch.Groups[1].Value}/{adoMatch.Groups[2].Value}/_git/{adoMatch.Groups[3].Value}";

            // Azure DevOps legacy: https://{account}.visualstudio.com/{project}/_apis/git/repositories/{repo}/items
            var vsoMatch = Regex.Match(urlTemplate,
                @"https://([^.]+)\.visualstudio\.com/([^/]+)/_apis/git/repositories/([^/?]+)");
            if (vsoMatch.Success)
                return $"https://{vsoMatch.Groups[1].Value}.visualstudio.com/{vsoMatch.Groups[2].Value}/_git/{vsoMatch.Groups[3].Value}";
        }
        return null;
    }

    /// <summary>
    /// Extracts the commit hash from SourceLink URL patterns.
    /// Handles both path-based (GitHub) and query-parameter-based (Azure DevOps) formats.
    /// </summary>
    public string? ExtractCommitHash()
    {
        foreach (var (_, urlTemplate) in _documentMappings)
        {
            // GitHub and others: commit hash as a path segment
            var pathMatch = Regex.Match(urlTemplate, @"/([0-9a-f]{40})(?:/|$|\?)", RegexOptions.IgnoreCase);
            if (pathMatch.Success)
                return pathMatch.Groups[1].Value;

            // Azure DevOps: version={commitHash} query parameter
            var queryMatch = Regex.Match(urlTemplate, @"[?&]version=([0-9a-f]{40})(?:&|$)", RegexOptions.IgnoreCase);
            if (queryMatch.Success)
                return queryMatch.Groups[1].Value;
        }
        return null;
    }

    /// <summary>
    /// Converts a raw.githubusercontent.com URL to a github.com browse URL.
    /// </summary>
    public static string? ConvertToGitHubBrowseUrl(string? rawUrl)
    {
        if (rawUrl == null) return null;

        var match = Regex.Match(rawUrl,
            @"https://raw\.githubusercontent\.com/([^/]+)/([^/]+)/([^/]+)/(.+)");
        if (match.Success)
            return $"https://github.com/{match.Groups[1].Value}/{match.Groups[2].Value}/raw/{match.Groups[3].Value}/{match.Groups[4].Value}";

        return rawUrl;
    }

    /// <summary>
    /// Converts an Azure DevOps items API URL to an Azure DevOps web browse URL.
    /// Handles both dev.azure.com and legacy *.visualstudio.com hosts.
    /// Returns the original URL unchanged if it is not a recognised Azure DevOps API URL.
    /// </summary>
    public static string? ConvertToAzureDevOpsBrowseUrl(string? apiUrl)
    {
        if (apiUrl == null) return null;

        string? filePath = ExtractQueryParam(apiUrl, "path");
        string? version = ExtractQueryParam(apiUrl, "version");

        // dev.azure.com/{org}/{project}/_apis/git/repositories/{repo}/items
        var adoMatch = Regex.Match(apiUrl,
            @"https://dev\.azure\.com/([^/]+)/([^/]+)/_apis/git/repositories/([^/?]+)");
        if (adoMatch.Success)
        {
            string browseUrl = $"https://dev.azure.com/{adoMatch.Groups[1].Value}/{adoMatch.Groups[2].Value}/_git/{adoMatch.Groups[3].Value}";
            return AppendAzureDevOpsBrowseParams(browseUrl, filePath, version);
        }

        // {account}.visualstudio.com/{project}/_apis/git/repositories/{repo}/items
        var vsoMatch = Regex.Match(apiUrl,
            @"https://([^.]+)\.visualstudio\.com/([^/]+)/_apis/git/repositories/([^/?]+)");
        if (vsoMatch.Success)
        {
            string browseUrl = $"https://{vsoMatch.Groups[1].Value}.visualstudio.com/{vsoMatch.Groups[2].Value}/_git/{vsoMatch.Groups[3].Value}";
            return AppendAzureDevOpsBrowseParams(browseUrl, filePath, version);
        }

        return apiUrl;
    }

    private static string? ExtractQueryParam(string url, string name)
    {
        int q = url.IndexOf('?');
        if (q < 0) return null;

        ReadOnlySpan<char> query = url.AsSpan(q + 1);
        string prefix = name + "=";

        foreach (var segment in query.Split('&'))
        {
            var part = query[segment];
            if (part.StartsWith(prefix, StringComparison.Ordinal))
                return Uri.UnescapeDataString(new string(part.Slice(prefix.Length)));
        }
        return null;
    }

    private static string AppendAzureDevOpsBrowseParams(string browseUrl, string? filePath, string? version)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(filePath))
            parts.Add($"path={Uri.EscapeDataString(filePath)}");
        if (!string.IsNullOrEmpty(version))
            parts.Add($"version=GC{Uri.EscapeDataString(version)}");
        return parts.Count > 0 ? $"{browseUrl}?{string.Join("&", parts)}" : browseUrl;
    }

    /// <summary>
    /// Extracts SourceLink JSON from a PDB metadata reader.
    /// </summary>
    internal static string? ExtractSourceLinkJson(MetadataReader reader)
    {
        foreach (CustomDebugInformationHandle handle in reader.CustomDebugInformation)
        {
            CustomDebugInformation info = reader.GetCustomDebugInformation(handle);
            Guid kind = reader.GetGuid(info.Kind);

            if (kind == SourceLinkGuid)
            {
                byte[] bytes = reader.GetBlobBytes(info.Value);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
        }

        return null;
    }

    private static Dictionary<string, string> ParseMappings(string sourceLinkJson)
    {
        Dictionary<string, string> mappings = [];

        try
        {
            using var doc = JsonDocument.Parse(sourceLinkJson);
            if (doc.RootElement.TryGetProperty("documents", out var documents))
            {
                foreach (var prop in documents.EnumerateObject())
                {
                    string? url = prop.Value.GetString();
                    if (url != null)
                        mappings[prop.Name] = url;
                }
            }
        }
        catch
        {
            // Return empty mappings on parse error
        }

        return mappings;
    }
}
