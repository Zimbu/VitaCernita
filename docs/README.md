# VitaCernita Documentation

Welcome to the VitaCernita documentation! VitaCernita is a high-performance cross-platform C# (.NET 9) Gmail filter management engine driven by a flexible Lua DSL.

---

## Architecture Overview

VitaCernita is structured around the official [Google Gmail API `users.settings.filters`](https://developers.google.com/workspace/gmail/api/reference/rest/v1/users.settings.filters) resource specification, keeping search criteria, message actions, and composite filters strictly separated and decoupled:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              GmailFilter                                │
│                                                                         │
│  id: "fin-001"                                                          │
│  name: "Vendor Invoices"                                                │
│                                                                         │
│  criteria (Query):                                                      │
│    from:billing@stripe.com filename:invoice.pdf                         │
│                                                                         │
│  action:                                                                │
│    addLabelIds: ["CATEGORY_PURCHASES", "Stripe"]                        │
│    removeLabelIds: ["INBOX"]                                            │
│    forward: "accounting@company.com"                                    │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Detailed Documentation Subfolders

Explore detailed documentation, comprehensive operator tables, and validation rules in each topic guide:

| Guide | Description | Key Topics |
| :--- | :--- | :--- |
| [**Search Queries**](queries/README.md) | Full reference for query conditions and search criteria | String matching (`from`, `to`, `subject`), 12 star & icon operators, media & attachments, status & state flags, locations, categories, size filtering, dates & relative durations, boolean logic (`And`, `Or`, `not`), input validation. |
| [**Filter Actions**](actions/README.md) | Actions applied to matching messages | `archive`, `mark_read`, `star`, `delete`, `mark_important`, category assignments, custom user labels, message forwarding, Gmail API mapping (`addLabelIds`, `removeLabelIds`, `forward`), system label protection. |
| [**Composite Filters**](filters/README.md) | Filters combining queries and actions | Filter structure, `.ToDictionary()` JSON serialization for the Gmail REST API, single vs multi-filter configurations, fluent `FilterBuilder`, and C# programmatic loader APIs. |
| [**Gmail Labels**](labels/README.md) | Custom mailbox label configuration | Label configuration matching `users.labels`, message list visibility, sidebar label list visibility, 102 predefined palette colors & aliases, JSON export. |

---

## Syntax Overview

VitaCernita supports three syntax flavors across queries, actions, filters, and labels:

1. **Functional Syntax**: Composable, expressive functions (`And(...)`, `Or(...)`, `not(...)`, `actions(...)`, `color(...)`).
2. **Fluent Builder Syntax**: Method chaining via `filter():...:build()`, `query():...:build()`, `action():...:build()`, and `label():...:build()`.
3. **Declarative Table Syntax**: Schema-first, pure Lua tables (`filter { ... }`, `query { ... }`, `action { ... }`, `label { ... }`).
