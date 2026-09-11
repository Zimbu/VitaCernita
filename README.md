# VitaCernita

> Cross-platform Core C# (.NET 9) Gmail filter management engine configured via Lua DSL.

VitaCernita pairs modern .NET performance with the flexibility of a declarative and functional Lua DSL for authoring, testing, and managing Gmail search filters. It mirrors Google's official [Gmail API `users.settings.filters`](https://developers.google.com/workspace/gmail/api/reference/rest/v1/users.settings.filters) resource specification, keeping search criteria and mailbox actions cleanly decoupled.

---

## Core Architecture: Query, Action & Filter

VitaCernita is built upon three decoupled pillars:

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

1. **Query** ([`IQueryCondition`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Queries/IQueryCondition.cs), [`GmailQuery`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Queries/GmailQuery.cs)):
   - Generates Gmail search criteria strings (e.g., `from:billing@stripe.com newer_than:30d`).
   - Supports 12 star icons, file/media metadata, inbox categories, size boundaries, date/duration math, and nested boolean logic (`And`, `Or`, `not`).
   - Completely decoupled: queries can be authored, validated, and evaluated independently of actions.

2. **Action** ([`GmailAction`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Actions/GmailAction.cs)):
   - Defines actions applied to matching messages (`archive`, `mark_read`, `star`, `mark_important`, `delete`, `add_category`, `add_label`, `forward_message`).
   - Maps 1-to-1 to Gmail's `addLabelIds`, `removeLabelIds`, and `forward` API payload fields.
   - Completely decoupled: actions can be authored, validated, and reused across filters.

3. **Filter** ([`GmailFilter`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Filters/GmailFilter.cs)):
   - The composite resource combining an optional `id`, a search `query` (criteria), and an `action`.
   - Exports directly to Google Gmail API payload format via `filter.ToDictionary()`.

---

## Detailed Documentation Subfolders

For complete operator tables, validation specifications, and API mappings, see the dedicated guides:

| Guide | Description | Key Topics |
| :--- | :--- | :--- |
| [**Search Queries Guide**](docs/queries/README.md) | Full query operator reference | String match fields, 12 stars/icons, media & attachments, status flags, folders, categories, size operators, dates & durations, boolean logic & negation, input validation rules. |
| [**Filter Actions Guide**](docs/actions/README.md) | Actions and system labels reference | Supported actions (`archive`, `star`, `delete`, etc.), Gmail API mapping, category enums, custom user labels vs system labels, forwarding validation. |
| [**Composite Filters Guide**](docs/filters/README.md) | Composite filter and serialization guide | Filter structure, `.ToDictionary()` JSON serialization for the Gmail REST API, multi-filter configurations, C# programmatic loader APIs, backwards compatibility. |

---

## Syntax Flavors

VitaCernita provides three syntax styles across queries, actions, and filters so you can use whichever best fits your workflow:

### 1. Functional DSL
Composable, declarative functions with nested boolean operators:
```lua
return filter {
    id = "sec-001",
    name = "Critical Security Escalations",
    query = And(
        From("secops@company.com"),
        Header("X-Severity", "CRITICAL"),
        Label("security-alerts"),
        not(Subject("drill"))
    ),
    action = actions(star, mark_important)
}
```

### 2. Fluent Builder API
Expressive method chaining for programmatically constructing filters:
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

### 3. Declarative Table Syntax
Schema-first, data-driven tables:
```lua
return filter {
    id = "sec-001",
    name = "Critical Security Escalations",
    query = {
        from = "secops@company.com",
        header = "X-Severity:CRITICAL",
        label = "security-alerts",
        ["not"] = { subject = "drill" }
    },
    action = {
        star = true,
        mark_important = true
    }
}
```

---

## High-Level Real-World Use Cases

Here is how VitaCernita solves common email workflow challenges:

### Use Case 1: Automated Receipt & Invoice Processing
Route invoices from SaaS vendors and cloud providers directly to accounting while keeping your inbox clean:
```lua
return filter {
    id = "fin-001",
    name = "Vendor Invoices & Receipts",
    query = And(
        Or(From("billing@aws.com"), From("invoicing@google.com"), From("billing@stripe.com")),
        Or(Filename("invoice.pdf"), Filename("receipt.pdf")),
        newer_than("90d")
    ),
    action = actions(
        archive,
        add_category('Purchases'),
        add_label('Accounting/Receipts'),
        forward_message('accounting@company.com')
    )
}
-- Generated Query : (from:billing@aws.com OR from:invoicing@google.com OR from:billing@stripe.com) (filename:invoice.pdf OR filename:receipt.pdf) newer_than:90d
-- Generated Action: removeLabelIds: ["INBOX"], addLabelIds: ["CATEGORY_PURCHASES", "Accounting/Receipts"], forward: "accounting@company.com"
```

### Use Case 2: Production Incident & Security Escalation
Ensure high-priority alerts are instantly visible with visual star icons and importance flags:
```lua
return filter {
    id = "ops-002",
    name = "Production Pager Alerts",
    query = And(
        From("alerts@pagerduty.com"),
        Or(has_red_bang, has_yellow_bang),
        newer_than("7d")
    ),
    action = actions(star, mark_important)
}
-- Generated Query : from:alerts@pagerduty.com newer_than:7d (has:red-bang OR has:yellow-bang)
-- Generated Action: addLabelIds: ["STARRED", "IMPORTANT"]
```

### Use Case 3: Newsletter & Marketing Cleanup
Automatically purge bulk promotional emails and automated marketing older than 30 days:
```lua
return filter {
    id = "clean-003",
    name = "Purge Stale Marketing",
    query = And(
        category_promotions,
        older_than("30d"),
        larger("1M")
    ),
    action = actions(delete)
}
-- Generated Query : category:promotions older_than:30d larger:1M
-- Generated Action: addLabelIds: ["TRASH"]
```

### Use Case 4: VIP & Executive Traffic with Negation
Highlight direct executive emails while cleanly filtering out calendar invitations and automated company digests using `not(...)`:
```lua
return filter {
    id = "exec-004",
    name = "Direct Executive Traffic",
    query = And(
        From("exec-team@company.com"),
        not(Or(Subject("calendar"), Subject("digest"), Subject("all-hands"))),
        not({ is_starred = true })
    ),
    action = actions(star, mark_important, add_label('VIP'))
}
-- Generated Query : from:exec-team@company.com -(subject:calendar OR subject:digest OR subject:all-hands) -is:starred
-- Generated Action: addLabelIds: ["STARRED", "IMPORTANT", "VIP"]
```

### Use Case 5: Mailing List & Developer Team Traffic
Route high-volume developer mailing lists to dedicated labels, skip the inbox, and mark as read:
```lua
return filter {
    id = "dev-005",
    name = "Engineering Mailing Lists",
    query = And(
        List("dev-announce@lists.company.com"),
        delivered_to("eng-oncall@company.com")
    ),
    action = actions(archive, mark_read, add_label('Lists/DevAnnounce'))
}
-- Generated Query : list:dev-announce@lists.company.com deliveredto:eng-oncall@company.com
-- Generated Action: removeLabelIds: ["INBOX", "UNREAD"], addLabelIds: ["Lists/DevAnnounce"]
```

---

## Gmail API Serialization

Every [`GmailFilter`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Filters/GmailFilter.cs) converts directly to the JSON payload required by Google's REST API using `.ToDictionary()`:

```json
{
  "id": "fin-001",
  "criteria": {
    "query": "(from:billing@aws.com OR from:billing@stripe.com) filename:invoice.pdf"
  },
  "action": {
    "addLabelIds": ["CATEGORY_PURCHASES", "Accounting/Receipts"],
    "removeLabelIds": ["INBOX"],
    "forward": "accounting@company.com"
  }
}
```

---

## Getting Started

### Build & Test

```bash
# Build solution
dotnet build

# Run unit tests
dotnet test

# Run CLI with default sample configuration
dotnet run --project src/VitaCernita

# Load a custom filter configuration
dotnet run --project src/VitaCernita -- -c path/to/filter.lua

# Render queries with explicit 'AND' keyword
dotnet run --project src/VitaCernita -- --explicit-and
```

---

## Managing the Repo with Jujutsu (`jj`)

This repository is colocated (`.jj/` and `.git/`). All version control operations can be performed using `jj`:

```bash
# Check working copy status
jj st

# View revision history
jj log

# Create a new change
jj new

# Describe / commit message
jj describe -m "feat: your change description"

# Update main bookmark to current revision
jj bookmark set main -r @

# Push bookmark to GitHub
jj git push --bookmark main
```
