# Actions Reference & System Labels

In VitaCernita, an **Action** defines the operations executed by Gmail when an incoming or existing message matches a filter's query criteria. Actions map directly to the `action` object of the [Google Gmail API `users.settings.filters`](https://developers.google.com/workspace/gmail/api/reference/rest/v1/users.settings.filters) resource.

---

## Table of Contents

1. [Supported Actions](#supported-actions)
2. [Google Gmail API Mapping](#google-gmail-api-mapping)
3. [Categories Reference](#categories-reference)
4. [Custom User Labels vs. System Labels](#custom-user-labels-vs-system-labels)
5. [Syntax Options](#syntax-options)
6. [Validation Rules](#validation-rules)

---

## Supported Actions

VitaCernita provides intuitive DSL functions for all filter operations supported by the Gmail API:

| DSL Operator | Description | Gmail API Field | Label ID / Payload |
| :--- | :--- | :--- | :--- |
| `archive` | Skip the Inbox (archive the message) | `removeLabelIds` | `["INBOX"]` |
| `mark_unread` / `mark_read` | Mark message as read | `removeLabelIds` | `["UNREAD"]` |
| `star` | Star the message | `addLabelIds` | `["STARRED"]` |
| `delete` / `trash` | Move message to Trash | `addLabelIds` | `["TRASH"]` |
| `mark_important` | Mark message as important | `addLabelIds` | `["IMPORTANT"]` |
| `add_category(cat)` | Apply an inbox category | `addLabelIds` | `["CATEGORY_*"]` (see [Categories](#categories-reference)) |
| `add_label(name)` | Apply a custom user label | `addLabelIds` | `["<label-name>"]` |
| `add_labels(...)` | Apply multiple custom user labels | `addLabelIds` | `["<name1>", "<name2>"]` |
| `forward_message(addr)` | Forward message to an email address | `forward` | `"<email-address>"` |

---

## Google Gmail API Mapping

Gmail's REST API represents filter actions as:

```json
{
  "action": {
    "addLabelIds": ["STARRED", "CATEGORY_PURCHASES", "Tax-2026"],
    "removeLabelIds": ["INBOX", "UNREAD"],
    "forward": "accounting@company.com"
  }
}
```

VitaCernita constructs this exact structure through `action.ToDictionary()`:

- **`addLabelIds`**: String array containing system label IDs (`STARRED`, `TRASH`, `IMPORTANT`, `CATEGORY_*`) and custom user label names.
- **`removeLabelIds`**: String array containing system labels to remove (`INBOX` for archiving, `UNREAD` for marking as read).
- **`forward`**: Email address to which matching messages will be forwarded automatically.

---

## Categories Reference

Gmail groups inbox mail into six primary category labels. The `add_category` DSL accepts user-friendly names (case-insensitive) and maps them to official Gmail system category IDs:

| DSL Parameter | Gmail System Label ID | Description |
| :--- | :--- | :--- |
| `'Primary'` / `'Personal'` | `CATEGORY_PERSONAL` | Main inbox tab for personal and high-priority conversations |
| `'Purchases'` | `CATEGORY_PURCHASES` | Order confirmations, invoices, tracking updates, and receipts |
| `'Social'` | `CATEGORY_SOCIAL` | Messages from social media and media-sharing platforms |
| `'Updates'` | `CATEGORY_UPDATES` | Automated confirmations, bills, bank statements, and account updates |
| `'Forums'` | `CATEGORY_FORUMS` | Messages from online groups, discussion boards, and mailing lists |
| `'Promotions'` | `CATEGORY_PROMOTIONS` | Marketing offers, discounts, newsletters, and sales |

### Example
```lua
add_category('Purchases')   -- Maps to addLabelIds: ["CATEGORY_PURCHASES"]
add_category('Promotions')  -- Maps to addLabelIds: ["CATEGORY_PROMOTIONS"]
```

---

## Custom User Labels vs. System Labels

### User Labels (`add_label`)
Custom labels categorize and organize mail into your personal folders:
```lua
add_label('Clients/AcmeCorp')
add_labels('ProjectX', 'PendingReview')
```

### System Label Protection
Reserved Gmail system labels (`INBOX`, `UNREAD`, `STARRED`, `TRASH`, `SPAM`, `DRAFT`, `SENT`, `IMPORTANT`, `CHAT`) cannot be passed to `add_label()`. Attempting to do so triggers an `ActionValidationException`. Instead, use the dedicated DSL operations:
- To apply `STARRED` → use `star`
- To remove `INBOX` → use `archive`
- To remove `UNREAD` → use `mark_read`
- To apply `TRASH` → use `delete`
- To apply `IMPORTANT` → use `mark_important`

---

## Syntax Options

### 1. Functional Syntax (`actions(...)`)
Compose action elements as comma-separated arguments:
```lua
return actions(
    archive,
    star,
    mark_important,
    add_category('Purchases'),
    add_label('Invoices'),
    forward_message('accounting@company.com')
)
```

### 2. Fluent ActionBuilder Syntax (`action():...:build()`)
Chain action methods fluently:
```lua
return action()
    :archive()
    :star()
    :mark_important()
    :add_category('Purchases')
    :add_label('Invoices')
    :forward_message('accounting@company.com')
    :build()
```

### 3. Declarative Table Syntax (`action { ... }`)
Provide configuration properties in a table:
```lua
return action {
    archive = true,
    star = true,
    mark_important = true,
    add_category = 'Purchases',
    add_label = 'Invoices',
    forward = 'accounting@company.com'
}
```

---

## Validation Rules

VitaCernita enforces action validity before filters are built or executed:

1. **Non-Empty Actions**: At least one action property (`archive`, `star`, `add_label`, etc.) must be defined. An entirely empty action throws `ActionValidationException`.
2. **Forwarding Email Validity**: Email addresses supplied to `forward_message` or `forward` must be non-empty, contain a valid user and domain (`user@example.com`), and adhere to standard email syntax.
3. **Valid Category Enum**: The category argument must match one of the 6 supported categories. Unknown category strings are rejected.
4. **Valid Custom Labels**: Label names cannot be empty, cannot consist of whitespace only, and cannot collide with reserved Gmail system label names.
