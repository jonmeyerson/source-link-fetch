# SourceLinkFetch

Extract and verify SourceLink metadata from .NET assemblies.

```shell
dotnet add package SourceLinkFetch
```

## Features

- **SourceLink detection** — Check if an assembly has SourceLink metadata
- **URL resolution** — Map source file paths to repository URLs
- **Verification** — HTTP HEAD checks to confirm source URLs are accessible
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
    string status = result.IsAccessible ? "✓" : "✗";
    Console.WriteLine($"  {status} {result.FilePath}");
}
```

### Convert raw URLs to browsable URLs

Each host stores a raw/API URL in the SourceLink JSON. Use the
`ConvertTo*BrowseUrl` helpers to turn those into links a human can open.

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

**Always load tokens from environment variables or a secrets manager. Never
hardcode credentials in source code.**

#### GitHub

Requires a personal access token with `repo` scope (classic), or
`contents:read` (fine-grained).

```csharp
using var client = new HttpClient();
SourceLinkCredentials.ConfigureGitHub(client,
    Environment.GetEnvironmentVariable("GITHUB_TOKEN")!);
```

#### Azure DevOps (cloud and on-premises)

Requires a personal access token with at least `Code (Read)` scope.

```csharp
using var client = new HttpClient();
SourceLinkCredentials.ConfigureAzureDevOps(client,
    Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT")!);
```

#### GitLab

Requires a personal access token with `read_repository` scope.

```csharp
using var client = new HttpClient();
SourceLinkCredentials.ConfigureGitLab(client,
    Environment.GetEnvironmentVariable("GITLAB_TOKEN")!);
```

#### Bitbucket Cloud

Requires a Bitbucket username and an app password with
`Repositories: Read` permission (created under Personal settings → App passwords).

```csharp
using var client = new HttpClient();
SourceLinkCredentials.ConfigureBitbucketCloud(client,
    Environment.GetEnvironmentVariable("BITBUCKET_USER")!,
    Environment.GetEnvironmentVariable("BITBUCKET_APP_PASSWORD")!);
```

#### Bitbucket Server / Data Center

Requires an HTTP access token with `Repository read` permission
(Bitbucket Server 5.5+).

```csharp
using var client = new HttpClient();
SourceLinkCredentials.ConfigureBitbucketServer(client,
    Environment.GetEnvironmentVariable("BITBUCKET_TOKEN")!);
```

#### Gitea

Requires an API token or fine-grained personal access token with repository
read access.

```csharp
using var client = new HttpClient();
SourceLinkCredentials.ConfigureGitea(client,
    Environment.GetEnvironmentVariable("GITEA_TOKEN")!);
```

#### GitWeb or other Basic-auth servers

```csharp
using var client = new HttpClient();
SourceLinkCredentials.ConfigureBasicAuth(client,
    Environment.GetEnvironmentVariable("GIT_USER")!,
    Environment.GetEnvironmentVariable("GIT_PASSWORD")!);
```

#### Mixed providers

If documents span multiple hosts, create one `HttpClient` per host and
partition the documents by their `ResolvedUrl` before calling `VerifyAsync`.

```csharp
var documents = reader.EnumerateSourceDocuments().ToList();

using var githubClient = new HttpClient();
SourceLinkCredentials.ConfigureGitHub(githubClient, githubToken);

using var adoClient = new HttpClient();
SourceLinkCredentials.ConfigureAzureDevOps(adoClient, adoPat);

var githubDocs = documents.Where(d => d.ResolvedUrl?.Contains("githubusercontent.com") == true);
var adoDocs    = documents.Where(d => d.ResolvedUrl?.Contains("dev.azure.com") == true);

var results = (await Task.WhenAll(
    SourceLinkVerifier.VerifyAsync(githubDocs, githubClient),
    SourceLinkVerifier.VerifyAsync(adoDocs, adoClient)
)).SelectMany(r => r).ToList();
```

#### Auth scheme reference

| Provider | Scheme | Credential |
|---|---|---|
| GitHub | `Bearer {token}` | Personal access token |
| Azure DevOps | `Basic {base64(":{pat}")}` | Personal access token (empty username) |
| GitLab | `Bearer {token}` | Personal access token |
| Bitbucket Cloud | `Basic {base64("user:app_password")}` | App password |
| Bitbucket Server | `Bearer {token}` | HTTP access token |
| Gitea | `Bearer {token}` | API token |
| GitWeb / other | `Basic {base64("user:password")}` | Username + password |
