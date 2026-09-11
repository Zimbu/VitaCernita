# Query Reference & Search Operators

In VitaCernita, a **Query** represents the search criteria formatted into standard Gmail search syntax. Queries map directly to the `criteria.query` field of the [Google Gmail API `users.settings.filters`](https://developers.google.com/workspace/gmail/api/reference/rest/v1/users.settings.filters) resource and can be authored and tested independently from actions.

---

## Table of Contents

1. [Supported String Match Fields](#supported-string-match-fields)
2. [Star & Icon Operators](#star--icon-operators)
3. [Media, Document & Label Metadata](#media-document--label-metadata)
4. [Status & State Operators](#status--state-operators)
5. [Location & Folder Operators](#location--folder-operators)
6. [Category Operators](#category-operators)
7. [Message Size Operators](#message-size-operators)
8. [Date & Duration Operators](#date--duration-operators)
9. [Boolean Logic & Negation](#boolean-logic--negation)
10. [Input Validation Rules](#input-validation-rules)
11. [Syntax Options](#syntax-options)

---

## Supported String Match Fields

All standard Gmail string search operators are supported with consistent syntax:

| DSL Operator | Gmail Operator | Description | Lua Example | Generated Query |
| :--- | :--- | :--- | :--- | :--- |
| `From(...)` | `from:` | Sender email, name, or domain | `From("alice@example.com")` | `from:alice@example.com` |
| `To(...)` | `to:` | Primary recipient email or domain | `To("devs@company.com")` | `to:devs@company.com` |
| `Cc(...)` | `cc:` | Carbon copy recipient | `Cc("audit@company.com")` | `cc:audit@company.com` |
| `Bcc(...)` | `bcc:` | Blind carbon copy recipient | `Bcc("archive@company.com")` | `bcc:archive@company.com` |
| `Subject(...)` | `subject:` | Subject line text | `Subject("High CPU Alert")` | `subject:"High CPU Alert"` |
| `List(...)` | `list:` | Mailing list ID or address | `List("dev-announce@lists.com")` | `list:dev-announce@lists.com` |
| `Filename(...)` | `filename:` | Attachment filename or extension | `Filename("invoice.pdf")` | `filename:invoice.pdf` |
| `DeliveredTo(...)` / `delivered_to(...)` | `deliveredto:` | Delivered-to header address (aliases) | `delivered_to("ops-alias@company.com")` | `deliveredto:ops-alias@company.com` |
| `Rfc822MsgId(...)` / `rfc822msgid(...)` | `rfc822msgid:` | Message-ID header value | `rfc822msgid("msg-01@example.com")` | `rfc822msgid:msg-01@example.com` |
| `Header(name, val)` / `Header("name:val")` | `header:` | Custom MIME header match | `Header("X-Severity", "CRITICAL")` | `header:X-Severity:CRITICAL` |
| `Label(...)` | `label:` | User or system label | `Label("finance")` | `label:finance` |
| `match("phrase")` | `" "` | Exact word or phrase (double-quoted) | `match("confidential audit")` | `"confidential audit"` |

---

## Star & Icon Operators

Gmail supports 12 colored stars and status icons plus the general `is:starred` operator. All 12 icons emit official hyphenated Gmail search operators (`has:<star-or-icon>`):

| DSL Operator / Identifier | Gmail Operator | Icon Type | Description |
| :--- | :--- | :--- | :--- |
| `is_starred` / `is_starred()` | `is:starred` | Any Star | Matches messages with any of the 12 stars or status icons |
| `has_yellow_star` / `has_yellow_star()` | `has:yellow-star` | Star | Standard yellow star |
| `has_orange_star` / `has_orange_star()` | `has:orange-star` | Star | Orange star |
| `has_red_star` / `has_red_star()` | `has:red-star` | Star | Red star |
| `has_purple_star` / `has_purple_star()` | `has:purple-star` | Star | Purple star |
| `has_blue_star` / `has_blue_star()` | `has:blue-star` | Star | Blue star |
| `has_green_star` / `has_green_star()` | `has:green-star` | Star | Green star |
| `has_red_bang` / `has_red_bang()` | `has:red-bang` | Bang | Red exclamation mark (`!`) |
| `has_yellow_bang` / `has_yellow_bang()` | `has:yellow-bang` | Bang | Yellow exclamation mark (`!`) |
| `has_orange_guillemet` / `has_orange_guillemet()` | `has:orange-guillemet` | Guillemet | Orange double right arrow (`>>`) |
| `has_green_check` / `has_green_check()` | `has:green-check` | Check | Green checkmark (`✓`) |
| `has_blue_info` / `has_blue_info()` | `has:blue-info` | Info | Blue information mark (`i`) |
| `has_purple_question` / `has_purple_question()` | `has:purple-question` | Question | Purple question mark (`?`) |

---

## Media, Document & Label Metadata

Filter messages based on attached content types, Google Drive files, or user-defined labels:

| DSL Operator / Identifier | Gmail Operator | Description |
| :--- | :--- | :--- |
| `has_attachment` / `has_attachment()` / `attachment` | `has:attachment` | Messages containing any file attachment |
| `has_drive` / `has_drive()` / `drive` | `has:drive` | Messages containing Google Drive files or links |
| `has_document` / `has_document()` / `document` | `has:document` | Messages containing Google Docs |
| `has_spreadsheet` / `has_spreadsheet()` / `spreadsheet` | `has:spreadsheet` | Messages containing Google Sheets |
| `has_presentation` / `has_presentation()` / `presentation` | `has:presentation` | Messages containing Google Slides |
| `has_youtube` / `has_youtube()` / `youtube` | `has:youtube` | Messages containing embedded YouTube videos |
| `has_user_labels` / `has_user_labels()` / `user_labels` | `has:userlabels` | Messages with at least one custom user label applied |
| `has_no_user_labels` / `has_no_user_labels()` / `no_user_labels` | `has:nouserlabels` | Messages with no custom user labels applied |

---

## Status & State Operators

Filter messages by system state and status flags:

| DSL Operator / Identifier | Gmail Operator | Description |
| :--- | :--- | :--- |
| `is_unread` / `is_unread()` / `unread` | `is:unread` | Unread messages |
| `is_read` / `is_read()` / `read` | `is:read` | Read messages |
| `is_important` / `is_important()` / `important` | `is:important` | Messages marked as important by Gmail or user |
| `is_starred` / `is_starred()` / `starred` | `is:starred` | Starred messages (any star) |
| `is_muted` / `is_muted()` / `muted` | `is:muted` | Muted conversations |
| `is_snoozed` / `is_snoozed()` / `snoozed` | `is:snoozed` | Snoozed conversations |
| `is_chat` / `is_chat()` / `chat` | `is:chat` | Google Chat / Hangouts messages |
| `is_draft` / `is_draft()` / `draft` | `is:draft` | Draft messages |
| `is_sent` / `is_sent()` / `sent` | `is:sent` | Sent messages |
| `is_trash` / `is_trash()` / `trash` | `is:trash` | Messages in Trash |
| `is_spam` / `is_spam()` / `spam` | `is:spam` | Messages in Spam |

---

## Location & Folder Operators

Filter messages by folder or mailbox location:

| DSL Operator / Identifier | Gmail Operator | Description |
| :--- | :--- | :--- |
| `in_anywhere` / `anywhere` | `in:anywhere` | Searches everywhere across Gmail (including Trash & Spam) |
| `in_archive` / `archive` | `in:archive` | Archived messages (messages outside the Inbox) |
| `in_snoozed` | `in:snoozed` | Snoozed messages |
| `in_inbox` / `inbox` | `in:inbox` | Messages currently in the Inbox |
| `in_sent` | `in:sent` | Messages in Sent Mail |
| `in_drafts` / `drafts` | `in:drafts` | Messages in Drafts folder |
| `in_trash` / `trash` | `in:trash` | Messages in Trash / Bin |
| `in_spam` / `spam` | `in:spam` | Messages in Spam |
| `in_chats` / `chats` | `in:chats` | Chat messages |

---

## Category Operators

Filter messages by Gmail's automatic inbox categories:

| DSL Operator / Identifier | Gmail Operator | Description |
| :--- | :--- | :--- |
| `category_primary` / `category('primary')` | `category:primary` | Primary inbox tab |
| `category_social` / `category('social')` | `category:social` | Social network updates and notifications |
| `category_promotions` / `category('promotions')` | `category:promotions` | Marketing, deals, and promotional emails |
| `category_updates` / `category('updates')` | `category:updates` | Automated confirmations, receipts, and bills |
| `category_forums` / `category('forums')` | `category:forums` | Messages from online groups, lists, and discussion forums |
| `category_reservations` / `category('reservations')` | `category:reservations` | Flight, hotel, and restaurant reservations |
| `category_purchases` / `category('purchases')` | `category:purchases` | Order confirmations, tracking, and receipts |

---

## Message Size Operators

Filter messages based on total message size in bytes or formatted units (`K`, `M`, `G`):

| DSL Operator | Gmail Operator | Description | Example | Output |
| :--- | :--- | :--- | :--- | :--- |
| `size(val)` | `size:` | Messages larger than specified size | `size("10M")` | `size:10M` |
| `larger(val)` / `larger_than(val)` | `larger:` | Messages larger than specified size | `larger("5M")` | `larger:5M` |
| `smaller(val)` / `smaller_than(val)` | `smaller:` | Messages smaller than specified size | `smaller("2M")` | `smaller:2M` |

### Size Syntax Rules
- **Unit Normalization**: Automatically converts and normalizes case (e.g. `10mb` → `10M`, `500kb` → `500K`, `1gb` → `1G`).
- **Raw Bytes**: Direct numeric arguments or byte strings (e.g. `1000000` or `"1000B"` → `1000000`).
- **Validation**: Negative values, decimals, zero, and unknown unit suffixes are rejected.

---

## Date & Duration Operators

Filter messages by absolute date or relative time duration:

| DSL Operator | Gmail Operator | Output Format | Description | Example |
| :--- | :--- | :--- | :--- | :--- |
| `After(...)` / `after(...)` | `after:` | `yyyy/MM/dd` | Messages sent after specified date | `After("2026/01/15")` |
| `Before(...)` / `before(...)` | `before:` | `yyyy/MM/dd` | Messages sent before specified date | `Before("2026/12/31")` |
| `Older(...)` / `older(...)` | `older:` | `yyyy/MM/dd` | Alias for `before:` | `Older("2026/07/01")` |
| `Newer(...)` / `newer(...)` | `newer:` | `yyyy/MM/dd` | Alias for `after:` | `Newer("2026/03/01")` |
| `OlderThan(...)` / `older_than(...)` | `older_than:` | `Nd` / `Nm` / `Ny` | Messages older than relative duration | `older_than("90d")` |
| `NewerThan(...)` / `newer_than(...)` | `newer_than:` | `Nd` / `Nm` / `Ny` | Messages newer than relative duration | `newer_than("14d")` |

### Date Configuration & Formatting Rules
- **Default Accepted Formats**: `MM/dd/yyyy` and `yyyy/MM/dd` with `/`, `-`, or `.` delimiters (e.g., `10/25/2026`, `2026-10-25`).
- **Custom Global Date Format**: Setting `date_format = "MM-dd-YYYY"` in the root of your configuration enforces that exact pattern across all dates.
- **Gmail Canonical Output**: Regardless of input format, generated Gmail queries always produce the official `yyyy/MM/dd` format required by Google.
- **Durations**: Integer count followed by `d` (days), `m` (months), or `y` (years). Units are normalized to lowercase.

---

## Boolean Logic & Negation

### Conjunction (`And`)
Joins multiple criteria. In standard Gmail search syntax, space represents implicit `AND`:
```lua
And(From("boss@company.com"), Subject("Urgent"), is_unread)
-- Emits: from:boss@company.com subject:Urgent is:unread
```
Using `--explicit-and` flag renders:
```
from:boss@company.com AND subject:Urgent AND is:unread
```

### Disjunction (`Or`)
Joins criteria with the `OR` operator:
```lua
Or(From("alice@example.com"), From("bob@example.com"))
-- Emits: (from:alice@example.com OR from:bob@example.com)
```

### Negation (`not` / `-`)
Prefixes expressions with Gmail's negation operator `-`:
```lua
not(From("spammer@evil.com"))
-- Emits: -from:spammer@evil.com

not(Or(Subject("newsletter"), Subject("weekly digest")))
-- Emits: -(subject:newsletter OR subject:"weekly digest")

not({ is_starred = true })
-- Emits: -is:starred
```
- **Double Negation**: `not(not(From("a@b.com")))` → `-(-from:a@b.com)`
- **Validation**: Empty `not()`, `not({})`, or `not("")` is strictly rejected.

---

## Input Validation Rules

VitaCernita enforces strict validation at parse time before producing query strings:

1. **Non-Empty Strings**: All text fields (`from`, `to`, `subject`, `header`, `label`, etc.) reject empty strings and whitespace-only values.
2. **Email Address & Fragment Syntax**:
   - Accepted forms: full email (`user@domain.com`), display brackets (`"Alice <alice@domain.com>"`), domain fragments (`"@company.com"`), or user fragments (`"dev-team"`).
   - Character whitelist: `[a-zA-Z0-9._+%@-]`.
   - Rejects consecutive dots (`..`), multiple `@` signs, and illegal punctuation.
3. **Real Calendar Dates**: Validates real calendar dates against Gregorian rules (rejects invalid dates like `02/30/2026`).
4. **Header Formats**: Accepts either `Header("X-Header", "Value")` or `Header("X-Header:Value")`. Rejects empty header names or values.
5. **Size Suffixes**: Only `K`, `M`, `G` (or `B`/raw bytes) are permitted. Negative values and decimals are rejected.

---

## Syntax Options

### 1. Functional Syntax
Ideal for complex nested queries using composable functions:
```lua
return query {
    match = And(
        From("secops@company.com"),
        Or(has_red_bang, has_yellow_bang),
        not(Subject("drill"))
    )
}
```

### 2. Fluent QueryBuilder Syntax
Ideal for method-chaining workflows:
```lua
return query()
    :from("secops@company.com")
    :is_unread()
    :has_attachment()
    :newer_than("7d")
    :build()
```

### 3. Declarative Table Syntax
Ideal for data-driven, schema-first configurations:
```lua
return query {
    from = "billing@stripe.com",
    filename = "invoice.pdf",
    newer_than = "30d"
}
```
