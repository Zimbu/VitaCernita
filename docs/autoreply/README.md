# Auto-Reply Reference (Gmail VacationSettings)

In VitaCernita, **`AutoReply`** is the DSL name for Gmail's **`VacationSettings`** resource, representing the vacation responder settings in Gmail ([Google Gmail API `VacationSettings` reference](https://developers.google.com/workspace/gmail/api/reference/rest/v1/VacationSettings)).

`AutoReply` can be configured, validated, and diffed directly alongside search filters and labels in your local Lua configuration.

---

## Table of Contents

1. [Configurable Fields](#configurable-fields)
2. [Google Workspace Domain Restriction](#google-workspace-domain-restriction)
3. [Start and End Times](#start-and-end-times)
4. [Response Body and Subject Rules](#response-body-and-subject-rules)
5. [Syntax Options](#syntax-options)
6. [JSON Serialization (`ToDictionary` & `ToJson`)](#json-serialization-todictionary--tojson)
7. [Validation Rules](#validation-rules)
8. [Diffing & Dry-Run Engine](#diffing--dry-run-engine)
9. [REST API Endpoints](#rest-api-endpoints)

---

## Configurable Fields

| DSL Field | Aliases | Gmail API Field | Type | Description |
| :--- | :--- | :--- | :--- | :--- |
| `enabled` | `enable_auto_reply`, `enableAutoReply`, `enable` | `enableAutoReply` | `boolean` | Controls whether Gmail automatically replies to incoming messages. |
| `subject` | `response_subject`, `responseSubject` | `responseSubject` | `string?` | Text prepended to the subject line in vacation response emails. |
| `body` | `response_body_plain_text`, `responseBodyPlainText`, `plain_text`, `text` | `responseBodyPlainText` | `string?` | Response body in plain text format. |
| `html` | `response_body_html`, `responseBodyHtml`, `body_html` | `responseBodyHtml` | `string?` | Response body in HTML format. If both plain text and HTML are supplied, Gmail uses the HTML body. |
| `contacts_only` | `restrict_to_contacts`, `restrictToContacts`, `restrict_contacts` | `restrictToContacts` | `boolean` | Whether responses are sent only to senders who are in the user's contacts. Default: `false`. |
| `domain_only` | `restrict_to_domain`, `restrictToDomain`, `restrict_domain`, `workspace_only` | `restrictToDomain` | `boolean` | Whether responses are sent only to senders within the user's domain. **Google Workspace accounts only**. Default: `false`. |
| `start_date` | `start_time`, `startTime`, `startDate`, `start` | `startTime` | `string` (epoch ms) | Optional start time for auto-replies. Messages received before this time will not receive an auto-reply. |
| `end_date` | `end_time`, `endTime`, `endDate`, `end` | `endTime` | `string` (epoch ms) | Optional end time for auto-replies. Messages received after this time will not receive an auto-reply. |

---

## Google Workspace Domain Restriction

> [!IMPORTANT]
> The `restrictToDomain` (`domain_only`) setting is **only available for Google Workspace users** (accounts with custom organizational domain names). It is **not supported** by Google for standard consumer `@gmail.com` or `@googlemail.com` accounts.

Because VitaCernita supports both Google Workspace and standard Gmail accounts:
- The Lua DSL and `AutoReply` domain model allow `domain_only = true` without throwing a static validation error on load.
- When an operation is performed against a specific account (such as diffing, validating against a target user, or calling the API):
  - If the target account is identified as `@gmail.com` or `@googlemail.com` and `restrictToDomain` is `true`, VitaCernita detects the constraint violation and reports an account diagnostic error:
    ```text
    The 'restrictToDomain' setting is only valid for Google Workspace accounts,
    but target account 'user@gmail.com' is a standard Gmail account (@gmail.com).
    ```
  - When making a live API call to `PUT /users/{userId}/settings/vacation`, Google's API returns `400 Bad Request` if `restrictToDomain` is set on a consumer `@gmail.com` account. `FakeGmailApiClient` simulates this exact error behavior in testing.

---

## Start and End Times

Start and end times can be expressed in Lua in several convenient ways:

1. **Date string (YYYY-MM-DD or YYYY/MM/DD)**:
   ```lua
   start_date = "2026-10-01",
   end_date = "2026-10-15"
   ```
2. **ISO 8601 Timestamp**:
   ```lua
   start_time = "2026-10-01T08:00:00Z",
   end_time = "2026-10-15T18:00:00Z"
   ```
3. **Custom Date Format**:
   If `date_format = "MM-dd-YYYY"` is specified in your configuration, dates matching that format will be parsed accordingly:
   ```lua
   start_date = "10-01-2026",
   end_date = "10-15-2026"
   ```
4. **Numeric Timestamp**:
   - Values > 10,000,000,000 are treated as epoch milliseconds (e.g. `1760000000000`).
   - Values between 1,000,000,000 and 10,000,000,000 are treated as Unix epoch seconds and automatically converted to milliseconds.

---

## Response Body and Subject Rules

Per the Google Gmail API:
1. **At least one response field required when enabled**:
   In order to enable auto-replies (`enabled = true`), either `responseSubject`, `responseBodyPlainText`, or `responseBodyHtml` must be non-empty. Enabling auto-replies with neither subject nor body is invalid.
2. **HTML vs Plain Text Precedence**:
   If both `responseBodyPlainText` and `responseBodyHtml` are provided, Gmail uses the `responseBodyHtml` body.

---

## Syntax Options

### 1. Declarative Table Syntax

```lua
return {
    -- Configured Labels
    labels = {
        label { name = "Receipts" }
    },

    -- Configured Filters
    rules = {
        filter {
            query = From("billing@stripe.com"),
            action = actions(archive, add_label("Receipts"))
        }
    },

    -- Configured Auto-Reply (Vacation Responder)
    auto_reply = auto_reply {
        enabled = true,
        subject = "Out of Office: Annual Leave",
        body = "I am currently away on annual leave until October 15th.",
        html = "<p>I am currently away on <b>annual leave</b> until October 15th.</p>",
        contacts_only = true,
        domain_only = false,
        start_date = "2026-10-01",
        end_date = "2026-10-15"
    }
}
```

### 2. Fluent Builder Syntax

```lua
return auto_reply()
    :enable(true)
    :subject("Parental Leave")
    :body("I am on parental leave until January 2027.")
    :html("<p>I am on <b>parental leave</b> until January 2027.</p>")
    :contacts_only(false)
    :domain_only(true)
    :start("2026-11-01")
    :end_time("2027-01-01")
    :build()
```

### 3. Dedicated Auto-Reply Script

A `.lua` file can define only an `auto_reply` table or global:

```lua
auto_reply = {
    enabled = true,
    subject = "Holiday Break",
    body = "The office is closed until Monday."
}
```

---

## JSON Serialization (`ToDictionary` & `ToJson`)

`AutoReply.ToDictionary()` outputs keys matching Google's REST API schema:

```json
{
  "enableAutoReply": true,
  "responseSubject": "Out of Office: Annual Leave",
  "responseBodyPlainText": "I am currently away on annual leave.",
  "responseBodyHtml": "<p>I am currently away on <b>annual leave</b>.</p>",
  "restrictToContacts": true,
  "restrictToDomain": false,
  "startTime": "1759276800000",
  "endTime": "1760486400000"
}
```

---

## Validation Rules

The [`AutoReplyValidator`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/AutoReply/Validation/AutoReplyValidator.cs) enforces:

1. **Content presence when enabled**:
   If `enableAutoReply` is true, either `responseSubject`, `responseBodyPlainText`, or `responseBodyHtml` must not be empty.
2. **Epoch timestamp non-negativity**:
   `startTime` and `endTime` cannot be negative numbers.
3. **Start time precedes end time**:
   If both `startTime` and `endTime` are provided, `startTime <= endTime`.
4. **Account domain check**:
   `AutoReplyValidator.ValidateForAccount(autoReply, accountEmail)` verifies that `restrictToDomain` is not set on standard `@gmail.com` accounts.

---

## Diffing & Dry-Run Engine

Use [`AutoReplyDiffer`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/AutoReply/Diff/AutoReplyDiffer.cs) and [`GmailSourceDiffer`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Sources/GmailSourceDiffer.cs) to compare current mailbox responder settings against desired configuration:

```csharp
var currentSource = new ApiGmailSource(client, userId: "alice@company.com");
var desiredSource = new LuaGmailSource("config/gmail_filter.lua");

var diff = await GmailSourceDiffer.DiffAutoReplyAsync(
    currentSource,
    desiredSource,
    new AutoReplyDiffOptions { TargetAccount = "alice@company.com" });

Console.WriteLine(diff.ToDryRunReport());
```

Sample output:
```text
Auto-Reply (Vacation Responder) Diff:
  Status: ~ Update Auto-Reply (2 change(s)):
    ~ responseSubject: 'Old Subject' -> 'Out of Office: Annual Leave'
    ~ restrictToContacts: False -> True
```

---

## REST API Endpoints

Google Gmail REST API documentation:
- **Get Vacation Responder**:
  `GET https://gmail.googleapis.com/gmail/v1/users/{userId}/settings/vacation`
- **Update Vacation Responder**:
  `PUT https://gmail.googleapis.com/gmail/v1/users/{userId}/settings/vacation`
