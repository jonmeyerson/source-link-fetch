# SourceLinkFetch

Extract and verify SourceLink metadata from .NET assemblies.

```shell
dotnet add package SourceLinkFetch
```

## Features

- **SourceLink detection** — Check if an assembly has SourceLink metadata
- **URL resolution** — Map source file paths to repository URLs
- **Verification** — HTTP HEAD checks with detailed status (accessible, requires auth, forbidden, not found)
- **Private repository support** — Authenticate with all major Git hosts
- **AOT compatible** — No reflection, works with NativeAOT
- **Zero dependencies** — Only `System.Reflection.Metadata` (in-box)

## Supported hosts

| Host | Package |
|---|---|
| GitHub (github.com or Enterprise) | `Microsoft.SourceLink.GitHub` |
| Azure DevOps (dev.azure.com) | `Microsoft.SourceLink.AzureRepos.Git` |
| Azure DevOps legacy (*.visualstudio.com) | `Microsoft.SourceLink.AzureRepos.Git` |
| Azure DevOps Server (on-premises) | `Microsoft.SourceLink.AzureDevOpsServer.Git` |
| GitLab (gitlab.com or self-hosted) | `Microsoft.SourceLink.GitLab` |
| Bitbucket Cloud (bitbucket.org) | `Microsoft.SourceLink.Bitbucket.Git` |
| Bitbucket Server / Data Center | `Microsoft.SourceLink.Bitbucket.Git` |
| Gitea (self-hosted) | `Microsoft.SourceLink.Gitea` |
| GitWeb (self-hosted) | `Microsoft.SourceLink.GitWeb` |

## Usage

### Check for SourceLink

```csharp
using SourceLinkFetch;

using var reader = SourceLinkReader.Open("path/to/assembly.dll");

Console.WriteLine($"Has PDB: {reader.HasPdb}");
Console.WriteLine($"Has SourceLink: {reader.HasSourceLink}");
Console.WriteLine($"Repository: {reader.RepositoryUrl}");
Console.WriteLine($"Commit: {reader.CommitHash}");
```

### Enumerate source documents

```csharp
foreach (var doc in reader.EnumerateSourceDocuments())
{
    Console.WriteLine($"  {doc.FilePath}");
    Console.WriteLine($"    URL: {doc.ResolvedUrl}");
    Console.WriteLine($"    Embedded: {doc.IsEmbedded}");
}
```

### Verify source URLs are accessible

```csharp
using var client = new HttpClient();
var documents = reader.EnumerateSourceDocuments().ToList();
var results = await SourceLinkVerifier.VerifyAsync(documents, client);

foreach (var result in results)
{
    Console.WriteLine(result.Status switch
    {
        VerificationStatus.Accessible             => $"  ✓ {result.FilePath}",
        VerificationStatus.RequiresAuthentication => $"  ⚿ {result.FilePath} (401 — needs credentials)",
        VerificationStatus.Forbidden              => $"  ✗ {result.FilePath} (403 — insufficient scope)",
        VerificationStatus.NotFound               => $"  ✗ {result.FilePath} (404 — broken link)",
        VerificationStatus.NetworkError           => $"  ✗ {result.FilePath} ({result.Error})",
        _                                         => $"  ✗ {result.FilePath} (HTTP {result.HttpStatusCode})",
    });
}
```

`VerificationResult` also exposes `HttpStatusCode` (nullable `int`) and the
convenience property `IsAccessible` for simple pass/fail checks.

### Provider detection

`SourceLinkProviders.Detect` inspects a resolved URL and returns the matching
provider, giving you browse-URL conversion and auth configuration without
needing to know which host produced the URL.

```csharp
// Build a credential map — source token values from your secrets store
var credentialMap = new Dictionary<ISourceLinkProvider, SourceLinkCredential>
{
    [SourceLinkProviders.GitHub]         = SourceLinkCredential.Token(myGitHubPat),
    [SourceLinkProviders.AzureDevOps]    = SourceLinkCredential.Token(myAdoPat),
    // or for AAD/Entra OAuth:          = SourceLinkCredential.Bearer(myAadToken),
    [SourceLinkProviders.GitLab]         = SourceLinkCredential.Token(myGitLabPat),
    [SourceLinkProviders.BitbucketCloud] = SourceLinkCredential.Bearer(myBitbucketApiToken),
    [SourceLinkProviders.BitbucketServer]= SourceLinkCredential.Token(myBitbucketServerToken),
    [SourceLinkProviders.Gitea]          = SourceLinkCredential.Token(myGiteaToken),
};

// One HttpClient per provider (each gets its own auth header)
var clients = credentialMap.ToDictionary(
    kvp => kvp.Key,
    kvp => {
        var client = new HttpClient();
        kvp.Key.ConfigureAuth(client, kvp.Value);
        return client;
    });

var documents = reader.EnumerateSourceDocuments().ToList();

// Convert to browse URLs
foreach (var doc in documents)
{
    var provider = SourceLinkProviders.Detect(doc.ResolvedUrl);
    string? browseUrl = provider?.ToBrowseUrl(doc.ResolvedUrl) ?? doc.ResolvedUrl;
    Console.WriteLine($"  {doc.FilePath} → {browseUrl}");
}

// Verify, routing each document to the right authenticated client
var results = await Task.WhenAll(
    documents
        .GroupBy(d => SourceLinkProviders.Detect(d.ResolvedUrl))
        .Select(g =>
        {
            var client = g.Key is not null && clients.TryGetValue(g.Key, out var c)
                ? c : new HttpClient();
            return SourceLinkVerifier.VerifyAsync(g, client);
        }));
```

The named providers on `SourceLinkProviders` (`GitHub`, `AzureDevOps`, etc.) are
the same instances used by `Detect`, so dictionary keying by reference works correctly.

#### Custom providers

Implement `ISourceLinkProvider` to support a host not covered above, then prepend
it to detection by building a custom list:

```csharp
IReadOnlyList<ISourceLinkProvider> allProviders =
[
    new MyInternalGitHubEnterpriseProvider(),
    .. SourceLinkProviders.All,  // built-ins as fallback
];

ISourceLinkProvider? provider = allProviders
    .FirstOrDefault(p => p.Matches(doc.ResolvedUrl));
```

### Convert raw URLs to browsable URLs

Each host stores a raw/API URL in the SourceLink JSON. Use the
`ConvertTo*BrowseUrl` helpers to turn those into links a human can open,
or call `provider.ToBrowseUrl(url)` when using provider detection.

```csharp
// GitHub:  raw.githubusercontent.com → github.com/…/raw/…
string? url = SourceLinkResolver.ConvertToGitHubBrowseUrl(doc.ResolvedUrl);

// GitLab:  /-/raw/ → /-/blob/
string? url = SourceLinkResolver.ConvertToGitLabBrowseUrl(doc.ResolvedUrl);

// Bitbucket Cloud:  /raw/ → /src/
string? url = SourceLinkResolver.ConvertToBitbucketCloudBrowseUrl(doc.ResolvedUrl);

// Bitbucket Server:  /raw/ → /browse/
string? url = SourceLinkResolver.ConvertToBitbucketServerBrowseUrl(doc.ResolvedUrl);

// Gitea:  /raw/commit/ → /src/commit/
string? url = SourceLinkResolver.ConvertToGiteaBrowseUrl(doc.ResolvedUrl);

// GitWeb:  a=blob_plain → a=blob
string? url = SourceLinkResolver.ConvertToGitWebBrowseUrl(doc.ResolvedUrl);

// Azure DevOps (all variants):  _apis/git/repositories/…/items → _git/…
string? url = SourceLinkResolver.ConvertToAzureDevOpsBrowseUrl(doc.ResolvedUrl);
```

### Private repositories

`SourceLinkVerifier` uses whichever `HttpClient` you supply, so authentication
is handled by configuring that client before passing it in. Credentials are
sent as HTTP headers — never embedded in URLs.

**Obtain token values from a secrets manager or secure configuration store.
Never hardcode credentials in source code.**

#### Credential kinds

| Kind | Factory | Use when |
|---|---|---|
| `Token` | `SourceLinkCredential.Token(pat)` | A personal access token (PAT). Provider determines encoding — most use Bearer; Azure DevOps uses Basic. |
| `Bearer` | `SourceLinkCredential.Bearer(token)` | An OAuth, AAD, or Entra access token. Always sent as `Authorization: Bearer`. |
| `Basic` | `SourceLinkCredential.Basic(user, pass)` | A username and password or app password. |

#### Auth scheme reference

| Provider | Accepted credentials | Wire encoding |
|---|---|---|
| GitHub | `Token`, `Bearer` | `Authorization: Bearer {token}` |
| GitLab | `Token`, `Bearer` | `Authorization: Bearer {token}` |
| Bitbucket Cloud | `Bearer`, `Basic`¹ | Bearer → `Authorization: Bearer {token}`; Basic → `Authorization: Basic {base64(user:pass)}` |
| Bitbucket Server | `Token`, `Bearer`, `Basic` | Token/Bearer → `Authorization: Bearer {token}`; Basic → `Authorization: Basic {base64(user:pass)}` |
| Gitea | `Token`, `Bearer`, `Basic` | Token/Bearer → `Authorization: Bearer {token}`; Basic → `Authorization: Basic {base64(user:pass)}` |
| GitWeb | `Basic` | `Authorization: Basic {base64(user:pass)}` |
| Azure DevOps | `Token`, `Bearer` | Token (PAT) → `Authorization: Basic {base64(":{pat}")}` ; Bearer (OAuth) → `Authorization: Bearer {token}` |

¹ Bitbucket Cloud app passwords (`Basic`) are deprecated and stop working June 2026. Use API tokens (`Bearer`) instead.

#### Per-provider convenience methods

```csharp
// GitHub — PAT
SourceLinkCredentials.ConfigureGitHub(client, pat);

// Azure DevOps — PAT
SourceLinkCredentials.ConfigureAzureDevOps(client, pat);

// Azure DevOps — AAD/Entra OAuth token
SourceLinkCredentials.ConfigureAzureDevOpsOAuth(client, aadToken);

// GitLab — PAT with read_repository scope
SourceLinkCredentials.ConfigureGitLab(client, pat);

// Bitbucket Cloud — API token (current standard)
SourceLinkCredentials.ConfigureBitbucketCloudToken(client, apiToken);

// Bitbucket Server — HTTP access token
SourceLinkCredentials.ConfigureBitbucketServer(client, token);

// Gitea — API token
SourceLinkCredentials.ConfigureGitea(client, token);

// GitWeb or any Basic-auth server
SourceLinkCredentials.ConfigureBasicAuth(client, username, password);
```
