# Composite Filters & Gmail API Serialization

In VitaCernita, a **Filter** is the composite entity combining an identifier, search criteria (**Query**), and operations (**Action**). It maps 1-to-1 to Google's official [Gmail API `users.settings.filters` resource](https://developers.google.com/workspace/gmail/api/reference/rest/v1/users.settings.filters).

---

## Table of Contents

1. [Filter Structure](#filter-structure)
2. [Google Gmail API Mapping](#google-gmail-api-mapping)
3. [JSON Serialization (`ToDictionary`)](#json-serialization-todictionary)
4. [Syntax Options](#syntax-options)
5. [Configuration File Structure](#configuration-file-structure)
6. [C# Programmatic API](#c-programmatic-api)
7. [Backwards Compatibility](#backwards-compatibility)

---

## Filter Structure

A `GmailFilter` consists of four properties:

| Property | Type | Description | Required |
| :--- | :--- | :--- | :--- |
| `Id` | `string?` | Unique identifier corresponding to Google's filter ID (e.g., `"sec-001"`). | Optional |
| `Name` | `string?` | Human-readable label or description for documentation and logging. | Optional |
| `Query` | `IQueryCondition?` | Search condition tree that generates Gmail's search query string. | Required for matching |
| `Action` | `GmailAction?` | Actions applied to incoming messages matching `Query`. | Optional |

---

## Google Gmail API Mapping

Google's Gmail API represents filters with the following schema:

```
users.settings.filters Resource:
┌─────────────────────────────────────────────────────────────┐
│  id: string                                                 │
│  criteria: {                                                │
│    query: string                                            │
│  }                                                          │
│  action: {                                                  │
│    addLabelIds: string[]                                    │
│    removeLabelIds: string[]                                 │
│    forward: string                                          │
│  }                                                          │
└─────────────────────────────────────────────────────────────┘
```

VitaCernita produces this payload directly from any `GmailFilter` instance.

---

## JSON Serialization (`ToDictionary`)

The `.ToDictionary()` method serializes a filter into a standard C# dictionary suitable for JSON serialization with `System.Text.Json` or direct REST API requests:

### Lua Definition
```lua
return filter {
    id = "fin-001",
    name = "Stripe Invoices",
    query = And(
        From("billing@stripe.com"),
        Filename("invoice.pdf")
    ),
    action = actions(
        archive,
        add_category('Purchases'),
        add_label('Stripe'),
        forward_message('accounting@company.com')
    )
}
```

### Serialized JSON Output
```json
{
  "id": "fin-001",
  "criteria": {
    "query": "from:billing@stripe.com filename:invoice.pdf"
  },
  "action": {
    "addLabelIds": [
      "CATEGORY_PURCHASES",
      "Stripe"
    ],
    "removeLabelIds": [
      "INBOX"
    ],
    "forward": "accounting@company.com"
  }
}
```

---

## Syntax Options

VitaCernita allows you to define filters using three distinct styles:

### 1. Declarative Table Syntax (`filter { ... }`)
```lua
return filter {
    id = "sec-001",
    name = "Critical Security Escalations",
    query = And(
        From("secops@company.com"),
        Header("X-Severity", "CRITICAL"),
        Label("security-alerts")
    ),
    action = actions(star, mark_important)
}
```

### 2. Fluent FilterBuilder Syntax (`filter():...:build()`)
```lua
return filter()
    :id("sec-001")
    :name("Critical Security Escalations")
    :from("secops@company.com")
    :header("X-Severity", "CRITICAL")
    :label("security-alerts")
    :actions(star, mark_important)
    :build()
```

### 3. Mixed / Unnested Syntax
When you define criteria fields directly inside `filter { ... }`, VitaCernita automatically extracts them into the `query`:
```lua
return filter {
    id = "ops-002",
    from = "pagerduty.com",
    has_red_bang = true,
    action = actions(star, mark_important)
}
```

---

## Configuration File Structure

VitaCernita supports single-filter scripts or multi-filter configuration files containing multiple filters, rules, and global settings:

```lua
return {
    -- Enforce a uniform date format for all dates across the entire configuration
    date_format = "MM-dd-YYYY",

    -- List of composite filters
    filters = {
        filter {
            id = "sec-001",
            name = "Confidential Security Alerts",
            query = And(
                From("secops@company.com"),
                match("unauthorized privilege escalation")
            ),
            action = actions(star, mark_important)
        },

        filter {
            id = "fin-002",
            name = "Vendor Invoices",
            query = And(
                Or(From("aws.com"), From("google.com")),
                Filename("invoice.pdf"),
                After("01-01-2026")
            ),
            action = actions(archive, add_category('Purchases'), add_label('Invoices'))
        }
    }
}
```

---

## C# Programmatic API

### Loading Filters
```csharp
using VitaCernita.Core.Filters;

// Load filters from a file
IReadOnlyList<GmailFilter> filters = await GmailFilterLoader.LoadFiltersFromFileAsync("config/gmail_filter.lua");

foreach (var filter in filters)
{
    Console.WriteLine($"ID: {filter.Id}");
    Console.WriteLine($"Query: {filter.ToGmailQuery()}");
    Console.WriteLine($"Actions: {filter.Action?.ToString()}");

    // Convert to Gmail API dictionary
    Dictionary<string, object> apiPayload = filter.ToDictionary();
}
```

### Loading Queries Independently
```csharp
using VitaCernita.Core.Queries;

// Load standalone query criteria
IQueryCondition query = await GmailQueryLoader.LoadQueryFromScriptAsync(@"
    return query {
        from = 'security@company.com',
        newer_than = '7d'
    }
");

string queryString = query.ToGmailQuery(); // "from:security@company.com newer_than:7d"
```

---

## Backwards Compatibility

Existing configurations and codebases relying on earlier VitaCernita releases remain 100% compatible:

- **`rule { ... }`**: Fully supported as an alias for a filter without an explicit `id`.
- **`filter.Criteria` & `filter.Condition`**: Supported as bidirectional aliases for `filter.Query`.
- **`rules = { ... }`**: Configuration files using `rules = { ... }` are loaded identically to `filters = { ... }`.
- **`IFilterCondition`**: Extends `IQueryCondition` so any existing custom condition works seamlessly across both query and filter contexts.
