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

---

### Media, Document & Label Metadata (`has:`)

| DSL Operator / Identifier | Gmail Operator | Description |
| :--- | :--- | :--- |
| `has_attachment` / `has_attachment()` / `attachment` | `has:attachment` | Messages with file attachments |
| `has_drive` / `has_drive()` / `drive` | `has:drive` | Messages with Google Drive links or attachments |
| `has_document` / `has_document()` / `document` | `has:document` | Messages with Google Docs |
| `has_spreadsheet` / `has_spreadsheet()` / `spreadsheet` | `has:spreadsheet` | Messages with Google Sheets |
| `has_presentation` / `has_presentation()` / `presentation` | `has:presentation` | Messages with Google Slides |
| `has_youtube` / `has_youtube()` / `youtube` | `has:youtube` | Messages containing YouTube videos |
| `has_user_labels` / `has_user_labels()` / `user_labels` | `has:userlabels` | Messages with user-defined labels |
| `has_no_user_labels` / `has_no_user_labels()` / `no_user_labels` | `has:nouserlabels` | Messages without any user-defined labels |

---

### Status & State Operators (`is:`)

| DSL Operator / Identifier | Gmail Operator | Description |
| :--- | :--- | :--- |
| `is_unread` / `is_unread()` / `unread` | `is:unread` | Unread messages |
| `is_read` / `is_read()` / `read` | `is:read` | Read messages |
| `is_important` / `is_important()` / `important` | `is:important` | Messages marked as important |
| `is_starred` / `is_starred()` / `starred` | `is:starred` | Starred messages |
| `is_muted` / `is_muted()` / `muted` | `is:muted` | Muted conversations |
| `is_snoozed` / `is_snoozed()` / `snoozed` | `is:snoozed` | Snoozed conversations |
| `is_chat` / `is_chat()` / `chat` | `is:chat` | Google Chat messages |
| `is_draft` / `is_draft()` / `draft` | `is:draft` | Draft messages |
| `is_sent` / `is_sent()` / `sent` | `is:sent` | Sent messages |
| `is_trash` / `is_trash()` / `trash` | `is:trash` | Messages in Trash |
| `is_spam` / `is_spam()` / `spam` | `is:spam` | Messages in Spam |

---

### Location & Folder Operators (`in:`)

| DSL Operator / Identifier | Gmail Operator | Description |
| :--- | :--- | :--- |
| `in_anywhere` / `anywhere` | `in:anywhere` | Searches everywhere across Gmail (including Trash & Spam) |
| `in_archive` / `archive` | `in:archive` | Archived messages (outside Inbox) |
| `in_snoozed` | `in:snoozed` | Snoozed messages |
| `in_inbox` / `inbox` | `in:inbox` | Messages located in the Inbox |
| `in_sent` | `in:sent` | Messages in Sent Mail |
| `in_drafts` / `drafts` | `in:drafts` | Messages in Drafts |
| `in_trash` / `trash` | `in:trash` | Messages in Trash / Bin |
| `in_spam` / `spam` | `in:spam` | Messages in Spam |
| `in_chats` / `chats` | `in:chats` | Chat messages |

---

### Category Operators (`category:`)

| DSL Operator / Identifier | Gmail Operator | Description |
| :--- | :--- | :--- |
| `category_primary` / `category('primary')` | `category:primary` | Messages in Primary inbox tab |
| `category_social` / `category('social')` | `category:social` | Messages from social networks |
| `category_promotions` / `category('promotions')` | `category:promotions` | Promotional offers and marketing |
| `category_updates` / `category('updates')` | `category:updates` | Automated confirmations and updates |
| `category_forums` / `category('forums')` | `category:forums` | Messages from discussion forums / groups |
| `category_reservations` / `category('reservations')` | `category:reservations` | Flight, hotel, and dining reservations |
| `category_purchases` / `category('purchases')` | `category:purchases` | Order confirmations, tracking, and receipts |

---

### Message Size Operators (`size:`, `larger:`, `smaller:`)

Filter messages based on message size in bytes or formatted units (`K`, `M`, `G`):

| DSL Operator | Gmail Operator | Description | Example |
| :--- | :--- | :--- | :--- |
| `size(val)` | `size:` | Messages larger than specified size | `size("10M")`, `size(1000000)` |
| `larger(val)` / `larger_than(val)` | `larger:` | Messages larger than specified size | `larger("5M")`, `larger_than("500K")` |
| `smaller(val)` / `smaller_than(val)` | `smaller:` | Messages smaller than specified size | `smaller("2M")`, `smaller_than("100K")` |

- **Unit Normalization**: Automatically converts units (`10mb` -> `10M`, `500kb` -> `500K`, `1gb` -> `1G`).
- **Raw Bytes**: Direct numeric arguments or byte strings (e.g. `1000000` or `"1000B"` -> `1000000`).
- **Strict Validation**: Rejects invalid units, negative values, decimals, zero, and empty strings.

---

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

#### 2. Negation Operator (`not` / `-`)
Negate single fields, exact phrases, star operators, or composite logical expressions using `-`:
```lua
return rule {
    name = "Exclude Executive Noise",
    match = And(
        From("exec-team@company.com"),
        not({ is_starred = true }),
        not(Or(Subject("automated"), Subject("newsletter")))
    )
}
-- Emits: from:exec-team@company.com -(subject:automated OR subject:newsletter) -is:starred
```
- **Single Fields & Operators**: `not(From("a@b.com"))` -> `-from:a@b.com`, `not(is_starred)` -> `-is:starred`
- **Negating `And` / `Or`**: `not(Or(To("a"), To("b")))` -> `-(to:a OR to:b)`
- **Double Negation**: `not(not(From("a")))` -> `-(-from:a)`
- **Validation**: Empty `not()`, `not({})`, or `not("")` is strictly rejected.

#### 3. Composite Nested Rules
Combine boolean operators (`And`, `Or`, `not`) at arbitrary depths:
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

#### 4. Declarative Table Syntax
Any rule can also be written in pure Lua table syntax:
```lua
return {
    rules = {
        {
            from = "cfo@company.com",
            ["not"] = { is_starred = true },
            ["or"] = {
                { filename = "dividend.pdf" },
                { filename = "sheet.xlsx" }
            }
        }
    }
}
```


---

### Action Language & System Labels

VitaCernita provides a decoupled action language matching the [Gmail API labels and filter actions guide](https://developers.google.com/workspace/gmail/api/guides/labels). The search criteria and actions are decoupled and can be used together or independently.

#### Supported Actions

| Action | Gmail API Mapping | Description |
|---|---|---|
| `archive` | `removeLabelIds: ["INBOX"]` | Removes the `INBOX` system label (skip the inbox). |
| `mark_unread` / `mark_read` | `removeLabelIds: ["UNREAD"]` | Removes the `UNREAD` system label (marks message as read). |
| `star` | `addLabelIds: ["STARRED"]` | Adds the `STARRED` system label. |
| `delete` / `trash` | `addLabelIds: ["TRASH"]` | Adds the `TRASH` system label (moves to trash). |
| `mark_important` | `addLabelIds: ["IMPORTANT"]` | Adds the `IMPORTANT` system label. |
| `add_category(cat)` | `addLabelIds: ["CATEGORY_*"]` | Applies one of 6 enumerated categories: `Primary` (`CATEGORY_PERSONAL`), `Purchases` (`CATEGORY_PURCHASES`), `Social` (`CATEGORY_SOCIAL`), `Updates` (`CATEGORY_UPDATES`), `Forums` (`CATEGORY_FORUMS`), `Promotions` (`CATEGORY_PROMOTIONS`). |
| `add_label(lbl)` / `add_labels(...)` | `addLabelIds: [lbl]` | Adds custom user labels. Rejects empty strings and reserved system label names. |
| `forward_message(email)` | `forward: email` | Forwards the message to a validated email address. |

#### Action Syntax Styles

**1. Functional Actions:**
```lua
return actions(
    archive,
    star,
    mark_important,
    add_category('Purchases'),
    add_label('Receipts'),
    forward_message('accounting@company.com')
)
```

**2. Fluent ActionBuilder:**
```lua
return action()
    :archive()
    :star()
    :mark_important()
    :add_category('Purchases')
    :add_label('Receipts')
    :forward_message('accounting@company.com')
    :build()
```

**3. Declarative Action Table:**
```lua
return action {
    archive = true,
    star = true,
    mark_important = true,
    add_category = 'Purchases',
    add_label = 'Receipts',
    forward = 'accounting@company.com'
}
```

#### Combining Query and Action

Filters combine search criteria and actions:
```lua
return filter {
    query = { from = 'billing@stripe.com' },
    action = actions(archive, add_category('Purchases'), add_label('Stripe'))
}
```

Or using the fluent builder:
```lua
return filter()
    :from('billing@stripe.com')
    :actions(archive, add_category('Purchases'))
    :build()
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
