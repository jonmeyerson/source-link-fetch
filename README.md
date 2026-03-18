# SourceLinkFetch

Extract and verify SourceLink metadata from .NET assemblies.

```shell
dotnet add package SourceLinkFetch
```

## Features

- **SourceLink detection** — Check if an assembly has SourceLink metadata
- **URL resolution** — Map source file paths to repository URLs
- **Verification** — HTTP HEAD checks to confirm source URLs are accessible
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
