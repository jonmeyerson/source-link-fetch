# SourceLinkFetch

Extract and verify SourceLink metadata from .NET assemblies.

```shell
dotnet add package SourceLinkFetch
```

## Features

- **SourceLink detection** — Check if an assembly has SourceLink metadata
- **URL resolution** — Map source file paths to repository URLs
- **Verification** — HTTP HEAD checks to confirm source URLs are accessible
- **Private repository support** — Authenticate with GitHub and Azure DevOps
- **AOT compatible** — No reflection, works with NativeAOT
- **Zero dependencies** — Only `System.Reflection.Metadata` (in-box)

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
    string status = result.IsAccessible ? "✓" : "✗";
    Console.WriteLine($"  {status} {result.FilePath}");
}
```

### Private repositories

`SourceLinkVerifier` uses whichever `HttpClient` you supply, so authentication
is handled by configuring that client before passing it in. Credentials are
sent as HTTP headers — never embedded in URLs.

**Always load tokens from environment variables or a secrets manager. Never
hardcode credentials in source code.**

#### GitHub (github.com or GitHub Enterprise)

Requires a personal access token with `repo` scope (classic), or
`contents:read` (fine-grained).

```csharp
using var client = new HttpClient();
SourceLinkCredentials.ConfigureGitHub(client,
    Environment.GetEnvironmentVariable("GITHUB_TOKEN")!);

var results = await SourceLinkVerifier.VerifyAsync(documents, client);
```

#### Azure DevOps (dev.azure.com, *.visualstudio.com, or on-premises)

Requires a personal access token with at least `Code (Read)` scope.

```csharp
using var client = new HttpClient();
SourceLinkCredentials.ConfigureAzureDevOps(client,
    Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT")!);

var results = await SourceLinkVerifier.VerifyAsync(documents, client);
```

#### How it works

| Provider | Auth scheme | Credential |
|---|---|---|
| GitHub | `Bearer {token}` | Personal access token |
| Azure DevOps | `Basic {base64(":{pat}")}` | Personal access token (empty username) |

If a single `HttpClient` needs to reach multiple providers (e.g. a build
fetching assemblies from both GitHub and Azure DevOps), create a separate
client per provider and partition the documents by their `ResolvedUrl` host
before calling `VerifyAsync`.
