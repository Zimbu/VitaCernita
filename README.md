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

*Note: Special enumerated operators `in:` and additional `is:` options will be introduced in future commits with dedicated enumerated DSL constructs.*

---

### Star & Icon Operators (`has:`, `is:starred`)

All 12 Gmail star and status icons plus the general `is:starred` operator are supported with multiple intuitive syntax forms. Per official Google Gmail search documentation, individual stars emit official hyphenated Gmail search operators (`has:<star-or-icon>`) and the general starred filter emits `is:starred`:

| DSL Operator / Identifier | Gmail Operator | Icon Type | Description |
| :--- | :--- | :--- | :--- |
| `is_starred` / `is_starred()` | `is:starred` | Any Star | Matches all starred messages across all 12 star and icon options |
| `has_yellow_star` / `has_yellow_star()` | `has:yellow-star` | Star | Standard yellow star |
| `has_orange_star` / `has_orange_star()` | `has:orange-star` | Star | Orange star |
| `has_red_star` / `has_red_star()` | `has:red-star` | Star | Red star |
| `has_purple_star` / `has_purple_star()` | `has:purple-star` | Star | Purple star |
| `has_blue_star` / `has_blue_star()` | `has:blue-star` | Star | Blue star |
| `has_green_star` / `has_green_star()` | `has:green-star` | Star | Green star |
| `has_red_bang` / `has_red_bang()` | `has:red-bang` | Bang | Red exclamation mark |
| `has_yellow_bang` / `has_yellow_bang()` | `has:yellow-bang` | Bang | Yellow exclamation mark |
| `has_orange_guillemet` / `has_orange_guillemet()` | `has:orange-guillemet` | Guillemet | Orange double right arrow (`>>`) |
| `has_green_check` / `has_green_check()` | `has:green-check` | Check | Green checkmark |
| `has_blue_info` / `has_blue_info()` | `has:blue-info` | Info | Blue information mark (`i`) |
| `has_purple_question` / `has_purple_question()` | `has:purple-question` | Question | Purple question mark (`?`) |

#### Flexible Expression Styles
- **All Starred Messages**: `is_starred`, `is_starred()`, `IsStarred`, or `is("starred")` emits `is:starred`.
- **Function Call or Identifier**: `has_red_bang()` or `has_red_bang` (without parentheses).
- **PascalCase**: `HasYellowStar`, `HasRedBang`, `HasOrangeGuillemet`, `IsStarred`, etc.
- **Generic Operator**: `has("yellow_star")` or `has("yellow-star")` (normalizes underscores and hyphens), and `is("starred")`.
- **FilterBuilder**: `filter():has_yellow_star():is_starred():from("boss@company.com")`.
- **Table Syntax**: `{ is_starred = true }`, `{ has_yellow_star = true }`, or `{ has = "red_bang" }`.
- **Strict Enumerated Validation**: Reject unsupported star or icon names with descriptive validation exceptions.

### Date & Duration Operators

| DSL Operator | Gmail Operator | Output Format | Description | Example |
| :--- | :--- | :--- | :--- | :--- |
| `After(...)` | `after:` | `yyyy/MM/dd` | Messages sent after specified date | `After("2026/01/15")` |
| `Before(...)` | `before:` | `yyyy/MM/dd` | Messages sent before specified date | `Before("2026/12/31")` |
| `Older(...)` | `older:` | `yyyy/MM/dd` | Alias for `before:` | `Older("2026/07/01")` |
| `Newer(...)` | `newer:` | `yyyy/MM/dd` | Alias for `after:` | `Newer("2026/03/01")` |
| `OlderThan(...)` | `older_than:` | `Nd` / `Nm` / `Ny` | Relative age older than duration | `older_than("90d")` |
| `NewerThan(...)` | `newer_than:` | `Nd` / `Nm` / `Ny` | Relative age newer than duration | `newer_than("14d")` |

#### Date Syntax & Global Configuration
- **Default Accepted Input Formats**: `MM/dd/yyyy` and `yyyy/MM/dd` (with `/`, `-`, or `.` delimiters). Times are strictly forbidden.
- **Custom Global Format**: Define `date_format = "MM-dd-YYYY"` at the root or within `settings` of the Lua configuration. When set, all dates throughout the entire configuration must match this exact format.
- **Gmail Canonical Output**: Regardless of how dates are provided in Lua, generated Gmail filter strings always use Gmail's required `yyyy/MM/dd` standard (4-digit year / 2-digit month / 2-digit day).
- **Durations**: Positive integer count paired with `d` (days), `m` (months), or `y` (years). Units are normalized to lowercase.

---

### Input Validation
VitaCernita performs rigorous input validation before building filters:
- **Non-Empty Strings**: Rejects empty strings or whitespace-only values across all fields.
- **Email Addresses & Fragments**: Validates sender/recipient fields (`from`, `to`, `cc`, `bcc`, `deliveredto`). Accepts full emails, display name brackets (`"Alice <alice@example.com>"`), or search fragments (e.g. `"@company.com"` or `"dev-team"`). Enforces at most one `@` symbol, character whitelist (`[a-zA-Z0-9._+%@-]`), and rejects consecutive dots (`..`).
- **Date Calendar Validity**: Enforces real calendar dates (rejects invalid dates such as `02/30/2026`).
- **Duration Unit Validation**: Rejects invalid units (e.g., `s`, `w`, `h`) or non-positive numbers (`0d`, `-5m`).


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
