# Gmail API Interface & Integration Guide

VitaCernita provides a native, decoupled interface to the [Google Workspace Gmail REST API](https://developers.google.com/workspace/gmail/api/reference/rest). It enables listing and getting live mailbox resources (`users.labels` and `users.settings.filters`), computing diffs between local Lua configurations and target Gmail accounts, and executing dry-run or live synchronization without external bloat.

---

## Table of Contents

1. [Protocol & Library Architecture](#protocol--library-architecture)
2. [Authentication Architecture](#authentication-architecture)
3. [API Client Contract (`IGmailApiClient`)](#api-client-contract-igmailapiclient)
4. [Native Core .NET Implementation (`HttpGmailApiClient`)](#native-core-net-implementation-httpgmailapiclient)
5. [In-Memory Fake for Testing (`FakeGmailApiClient`)](#in-memory-fake-for-testing-fakegmailapiclient)
6. [Queryable Sources & Agnostic Differ (`IGmailSource`, `GmailSourceDiffer`)](#queryable-sources--agnostic-differ-igmailsource-gmailsourcediffer)
7. [Account Diffing & Synchronization (`GmailAccountDiffer`)](#account-diffing--synchronization-gmailaccountdiffer)
8. [CLI Usage (`--diff` & `--mock`)](#cli-usage---diff----mock)

---

## Protocol & Library Architecture

### Evaluation of Libraries: Why Native Core .NET?

When connecting .NET applications to Google's Gmail API, two primary approaches exist:

| Criteria | Google Official SDK (`Google.Apis.Gmail.v1`) | Native Core .NET (`System.Net.Http.HttpClient` + `System.Text.Json`) |
| :--- | :--- | :--- |
| **Runtime Overhead** | Heavy dependency tree (legacy Discovery v1 generator, NewtonSoft.Json ties, internal legacy wrapping). | **Zero external dependencies**. Built directly on the modern .NET 9 BCL. |
| **Model Alignment** | Forces usage of generated `Google.Apis.Gmail.v1.Data` models, requiring awkward conversion layers. | **Direct 1-to-1 mapping** to VitaCernita's strongly typed domain models (`GmailLabel`, `GmailFilter`, `GmailAction`). |
| **Performance** | Overhead from discovery reflection and serialization layers. | High throughput, `SocketsHttpHandler` connection pooling, zero-allocation UTF-8 JSON parsing. |
| **Testability** | Hard to mock due to non-virtual and generated SDK client structures. | Clean, first-class interface (`IGmailApiClient`) with full in-memory fake (`FakeGmailApiClient`). |

**Design Choice**: VitaCernita uses native Core .NET `HttpClient` with `System.Text.Json` for HTTP communication protocols, keeping the library lightweight, modern, and aligned with Core .NET best practices.

---

## Authentication Architecture

Google's Gmail REST API authenticates requests using OAuth 2.0 Bearer tokens:
```http
GET /gmail/v1/users/me/labels HTTP/1.1
Host: gmail.googleapis.com
Authorization: Bearer <access_token>
```

VitaCernita decouples authentication via the [`IGmailTokenProvider`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Api/Auth/IGmailTokenProvider.cs) interface:

```csharp
namespace VitaCernita.Core.Api.Auth;

public interface IGmailTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
```

### Supported Token Providers

1. **`BearerTokenProvider`** (Built-in):
   - Accepts a token string directly, or reads from environment variable `GMAIL_ACCESS_TOKEN`.
   ```csharp
   var provider = new BearerTokenProvider("ya29.a0AfH6SM...");
   // Or read from environment variable GMAIL_ACCESS_TOKEN:
   var provider = new BearerTokenProvider();
   ```

2. **Google OAuth 2.0 PKCE / `Google.Apis.Auth`** (Optional Extension):
   - For interactive desktop login, `Google.Apis.Auth` (`GoogleWebAuthorizationBroker`) can be plugged in behind `IGmailTokenProvider` without coupling the HTTP transport to Google's client SDK.

---

## API Client Contract (`IGmailApiClient`)

The [`IGmailApiClient`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Api/IGmailApiClient.cs) interface specifies `list` and `get` operations for labels and filters:

```csharp
namespace VitaCernita.Core.Api;

public interface IGmailApiClient
{
    // Labels (users.labels)
    Task<IReadOnlyList<GmailLabel>> ListLabelsAsync(
        string userId = "me",
        bool onlyUserLabels = true,
        CancellationToken cancellationToken = default);

    Task<GmailLabel?> GetLabelAsync(
        string labelId,
        string userId = "me",
        CancellationToken cancellationToken = default);

    // Filters (users.settings.filters)
    Task<IReadOnlyList<GmailFilter>> ListFiltersAsync(
        string userId = "me",
        CancellationToken cancellationToken = default);

    Task<GmailFilter?> GetFilterAsync(
        string filterId,
        string userId = "me",
        CancellationToken cancellationToken = default);
}
```

---

## Native Core .NET Implementation (`HttpGmailApiClient`)

[`HttpGmailApiClient`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Api/HttpGmailApiClient.cs) executes direct HTTP requests against `https://gmail.googleapis.com/gmail/v1`:

```csharp
using var httpClient = new HttpClient();
var tokenProvider = new BearerTokenProvider();
var client = new HttpGmailApiClient(httpClient, tokenProvider);

// List user labels
var labels = await client.ListLabelsAsync("me", onlyUserLabels: true);

// Get specific filter
var filter = await client.GetFilterAsync("filter_001");
```

- Automatically parses list responses with system-label filtering.
- Returns `null` on `404 Not Found` for `GetLabelAsync` and `GetFilterAsync`.
- Throws [`GmailApiException`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Api/GmailApiException.cs) on HTTP error status codes.

---

## In-Memory Fake for Testing (`FakeGmailApiClient`)

[`FakeGmailApiClient`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Api/Fakes/FakeGmailApiClient.cs) is a fully self-contained in-memory fake designed for testing without live credentials or network calls:

```csharp
var fake = new FakeGmailApiClient();

// Seed initial remote state
fake.AddLabel(new GmailLabel("Receipts", id: "L1", messageListVisibility: "show"));
fake.AddLabel(new GmailLabel("OldUnusedLabel", id: "L2"));

// Test diffing or retrieval
var labels = await fake.ListLabelsAsync("me");
Assert.Equal(2, labels.Count);
Assert.Equal(1, fake.ListLabelsCallCount);

// Simulate API errors
fake.SimulatedHttpError = HttpStatusCode.Unauthorized;
await Assert.ThrowsAsync<GmailApiException>(() => fake.ListLabelsAsync());
```

---

## Queryable Sources & Agnostic Differ (`IGmailSource`, `GmailSourceDiffer`)

VitaCernita unifies all mailbox representations—whether live Gmail accounts, local Lua scripts, or test fixtures—under a source-agnostic **`IGmailSource`** interface exposing in-memory **`IQueryable<T>`** collections.

### The Unified Interface: `IGmailSource`

```csharp
namespace VitaCernita.Core.Sources;

public interface IGmailSource
{
    string Name { get; }
    Task<IQueryable<GmailLabel>> GetLabelsAsync(CancellationToken ct = default);
    Task<IQueryable<GmailFilter>> GetFiltersAsync(CancellationToken ct = default);
}
```

### Supported Source Implementations

| Implementation | Description | Use Cases |
| :--- | :--- | :--- |
| [`LuaGmailSource`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Sources/LuaGmailSource.cs) | Evaluates Lua configuration files or scripts. | Desired state specifications in version control. |
| [`ApiGmailSource`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Sources/ApiGmailSource.cs) | Adapts any `IGmailApiClient` into an `IQueryable` source. | Current live state of any Gmail account. |
| [`InMemoryGmailSource`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Sources/InMemoryGmailSource.cs) | Mutable in-memory collection. | Testing, ad-hoc composition, pipeline manipulation. |

### Source-Agnostic Diffing (`GmailSourceDiffer`)

Because both sides are `IGmailSource`, the diff engine is completely decoupled from the data transport:

```csharp
using VitaCernita.Core.Sources;

// 1. Agnostic Diff: Live account vs Local Lua
IGmailSource current = new ApiGmailSource(gmailClient, "user@company.com");
IGmailSource desired = new LuaGmailSource("config/gmail_filter.lua");
LabelSetDiff diff = await GmailSourceDiffer.DiffLabelsAsync(current, desired);

// 2. Cross-Account Migration: Account A vs Account B
IGmailSource staging = new ApiGmailSource(stagingClient, "staging@corp.com");
IGmailSource prod = new ApiGmailSource(prodClient, "prod@corp.com");
LabelSetDiff syncDiff = await GmailSourceDiffer.DiffLabelsAsync(staging, prod);
```

### Arbitrary Subsets via LINQ Predicates

Because each source produces `IQueryable<GmailLabel>`, standard C# LINQ predicates can filter what to compare before entering the differ:

```csharp
// Diff ONLY labels within the "Finance/" hierarchy
var financeDiff = await GmailSourceDiffer.DiffLabelsAsync(
    currentSource,
    desiredSource,
    currentFilter: q => q.Where(l => l.Name.StartsWith("Finance/")),
    desiredFilter: q => q.Where(l => l.Name.StartsWith("Finance/"))
);

// Diff ONLY labels with custom colors
var coloredDiff = await GmailSourceDiffer.DiffLabelsAsync(
    currentSource,
    desiredSource,
    currentFilter: q => q.Where(l => l.Color != null),
    desiredFilter: q => q.Where(l => l.Color != null)
);
```

---

## Account Diffing & Synchronization (`GmailAccountDiffer`)

[`GmailAccountDiffer`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Api/GmailAccountDiffer.cs) bridges `IGmailApiClient` and the diff engine:

```csharp
// Compare local Lua configuration against target Gmail account
var diff = await GmailAccountDiffer.DiffLabelsFromFileAsync(client, "config/gmail_filter.lua");

// Output summary & dry-run report
Console.WriteLine(diff.ToSummaryString());
Console.WriteLine(diff.ToDryRunReport());
```

---

## CLI Usage (`--diff` & `--mock`)

Run label diffs directly from the command line:

```bash
# 1. Test in mock mode with the in-memory fake client:
dotnet run --project src/VitaCernita -- --diff --mock

# 2. Diff against live account with bearer token from environment:
export GMAIL_ACCESS_TOKEN="ya29.a0AfH6SM..."
dotnet run --project src/VitaCernita -- --diff

# 3. Diff with explicit token and custom config:
dotnet run --project src/VitaCernita -- -c path/to/labels.lua --diff --token "ya29..."
```
