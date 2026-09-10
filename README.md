# VitaCernita

> Cross-platform Core C# (.NET 9) Gmail filter management engine configured via Lua DSL.

VitaCernita pairs modern .NET performance with the flexibility of a declarative and functional Lua DSL for managing Gmail search filters.

---

## Gmail Filter Configuration

### Supported String Match Fields & Operators

All standard Google Gmail API string search operators are supported with consistent match syntax:

| DSL Operator | Gmail Operator | Description | Example |
| :--- | :--- | :--- | :--- |
| `From(...)` | `from:` | Sender email or display name | `From("alice@example.com")` |
| `To(...)` | `to:` | Primary recipient email | `To("devs@company.com")` |
| `Cc(...)` | `cc:` | Carbon copy recipient | `Cc("audit@company.com")` |
| `Bcc(...)` | `bcc:` | Blind carbon copy recipient | `Bcc("archive@company.com")` |
| `Subject(...)` | `subject:` | Subject line text | `Subject("High CPU Alert")` |
| `List(...)` | `list:` | Mailing list ID or address | `List("dev-announce@lists.com")` |
| `Filename(...)` | `filename:` | Attachment filename or extension | `Filename("invoice.pdf")` |
| `DeliveredTo(...)` | `deliveredto:` | Delivered-to header address (aliases) | `delivered_to("ops-alias@company.com")` |
| `Rfc822MsgId(...)` | `rfc822msgid:` | Message-ID header value | `rfc822msgid("msg-01@example.com")` |
| `Header(name, val)` | `header:` | Custom MIME header match | `Header("X-Severity", "CRITICAL")` |
| `Label(...)` | `label:` | User or system label | `Label("finance")` |
| `match("phrase")` | `" "` | Exact word or phrase (double-quoted) | `match("confidential audit")` |

*Note: Special enumerated operators like `has:`, `is:`, and `in:` will be introduced in future commits with dedicated enumerated DSL constructs.*

---

### Logic & Nesting

#### 1. Exact Word or Phrase Match (`match`)
Per Google Gmail search documentation, exact phrase searches are double-quoted search terms:
```lua
return rule {
    match = And(
        From("secops@company.com"),
        Label("security-alerts"),
        match("unauthorized privilege escalation")
    )
}
-- Emits: from:secops@company.com label:security-alerts "unauthorized privilege escalation"
```

#### 2. Composite Nested Rules
Combine boolean operators (`And`, `Or`) at arbitrary depths:
```lua
return rule {
    name = "Tri-Team Incident Dispatcher",
    match = Or(
        And(From("secops@company.com"), Subject("Security Breach")),
        And(From("devops@company.com"), Subject("Cluster Outage")),
        And(From("netops@company.com"), Subject("BGP Route Leak"))
    )
}
-- Emits: (from:devops@company.com subject:"Cluster Outage") OR (from:netops@company.com subject:"BGP Route Leak") OR (from:secops@company.com subject:"Security Breach")
```

#### 3. Declarative Table Syntax
Any rule can also be written in pure Lua table syntax:
```lua
return {
    rules = {
        {
            from = "cfo@company.com",
            ["delivered-to"] = "finance@company.com",
            label = "executive",
            match = "Quarterly Dividend",
            ["or"] = {
                { filename = "dividend.pdf" },
                { filename = "sheet.xlsx" }
            }
        }
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

# Run and print the precise Gmail filter text
dotnet run --project src/VitaCernita

# Load a custom filter configuration
dotnet run --project src/VitaCernita -- -c path/to/filter.lua

# Render with explicit 'AND' keyword
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
