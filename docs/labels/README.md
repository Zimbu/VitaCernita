# Labels Reference & Gmail API Resource

In VitaCernita, a **Label** defines a custom user mailbox label matching the [Google Gmail API `users.labels` resource](https://developers.google.com/workspace/gmail/api/reference/rest/v1/users.labels). Labels can be defined, styled, and configured with visibility settings alongside your search filters.

---

## Table of Contents

1. [Configurable Fields](#configurable-fields)
2. [Message List Visibility](#message-list-visibility)
3. [Label List Visibility](#label-list-visibility)
4. [Label Color & Palette](#label-color--palette)
5. [Syntax Options](#syntax-options)
6. [JSON Serialization (`ToDictionary`)](#json-serialization-todictionary)
7. [Validation Rules](#validation-rules)
8. [Label Diffing & Dry-Run Engine](#label-diffing--dry-run-engine)

---

## Configurable Fields

| DSL Field | Gmail API Field | Type | Description |
| :--- | :--- | :--- | :--- |
| `id` | `id` | `string?` | Optional immutable ID for updating or referencing an existing label. |
| `name` | `name` | `string` | Display name of the label (e.g. `"Receipts"`, `"Clients/Acme"`). Required. |
| `message_list_visibility` / `messageListVisibility` | `messageListVisibility` | `string?` | Visibility of the label in the message list (`show`, `hide`). |
| `label_list_visibility` / `labelListVisibility` | `labelListVisibility` | `string?` | Visibility of the label in the sidebar label list (`labelShow`, `labelShowIfUnread`, `labelHide`). |
| `color` | `color` | `object?` | Text and background colors chosen from Google's allowed palette. |

> [!NOTE]
> System labels (e.g. `INBOX`, `TRASH`) cannot be created or modified via `users.labels.create`. Therefore, the `type` field is intentionally omitted from the DSL.

---

## Message List Visibility

Controls whether the label is displayed on messages in the main message list view:

| Value | Description |
| :--- | :--- |
| `'show'` | Show the label badge next to messages in the message list view. |
| `'hide'` | Hide the label badge from messages in the message list view. |

In the fluent builder, use `:show_in_message_list()` or `:hide_in_message_list()`.

---

## Label List Visibility

Controls whether the label is visible in the Gmail sidebar folder navigation:

| Value | Aliases | Description |
| :--- | :--- | :--- |
| `'labelShow'` | `'show'` | Always show the label in the sidebar list. |
| `'labelShowIfUnread'` | `'showIfUnread'`, `'show_if_unread'` | Show the label only if it contains unread messages. |
| `'labelHide'` | `'hide'` | Hide the label in the sidebar list (accessible under "More"). |

In the fluent builder, use `:show_in_label_list()`, `:show_if_unread()`, or `:hide_in_label_list()`.

---

## Label Color & Palette

Google's Gmail API strictly requires that both `textColor` and `backgroundColor` be selected from a predefined list of 102 hex values.

### Color Aliases
To maintain exact fidelity with the Gmail API, only common colors whose standard hex values appear in the official palette are supported as aliases:

| Alias | Hex Value | Allowed In Google Palette |
| :--- | :--- | :--- |
| `'black'` | `#000000` | Yes |
| `'white'` | `#ffffff` | Yes |

Any color names without an exact standard match in Google's palette (e.g., standard CSS `red` `#ff0000` or `blue` `#0000ff`) are skipped and rejected with a clear validation error. Use Google's specific hex codes below for custom colors.

### Predefined Allowed Hex Colors (102 Colors)

| Group | Allowed Hex Codes |
| :--- | :--- |
| **Monochrome & Grayscale** | `#000000`, `#434343`, `#464646`, `#666666`, `#999999`, `#cccccc`, `#c2c2c2`, `#e7e7e7`, `#efefef`, `#f3f3f3`, `#ffffff` |
| **Reds & Pinks** | `#822111`, `#8a1c0a`, `#ac2b16`, `#cc3a21`, `#e66550`, `#efa093`, `#f2b2a8`, `#f6c5be`, `#fb4c2f`, `#f691b3`, `#f691b2`, `#f7a7c0`, `#fbc8d9`, `#fcdee8`, `#fbd3e0`, `#e07798`, `#b65775`, `#994a64`, `#83334c`, `#711a36`, `#662e37`, `#cca6ac`, `#ebdbde` |
| **Oranges & Browns** | `#7a2e0b`, `#7a4706`, `#a46a21`, `#cf8933`, `#eaa041`, `#ff7537`, `#ffad46`, `#ffad47`, `#ffbc6b`, `#ffc8af`, `#ffd6a2`, `#ffdeb5`, `#ffe6c7` |
| **Yellows & Golds** | `#594c05`, `#684e07`, `#aa8831`, `#d5ae49`, `#f2c960`, `#fad165`, `#fbe983`, `#fcda83`, `#fce8b3`, `#fdedc1`, `#fef1d1` |
| **Greens** | `#04502e`, `#076239`, `#094228`, `#0b4f30`, `#0b804b`, `#149e60`, `#16a765`, `#16a766`, `#1a764d`, `#2a9c68`, `#3dc789`, `#42d692`, `#43d692`, `#44b984`, `#68dfa9`, `#89d3b2`, `#a0eac9`, `#a2dcc1`, `#b3efd3`, `#b9e4d0`, `#c6f3de` |
| **Cyans & Teals** | `#0d3b44`, `#2da2bb`, `#98d7e4` |
| **Blues** | `#0d3472`, `#1c4587`, `#285bac`, `#3c78d8`, `#4986e7`, `#4a86e8`, `#6d9eeb`, `#a4c2f4`, `#b6cff5`, `#c9daf8` |
| **Purples & Violets** | `#3d188e`, `#41236d`, `#653e9b`, `#8e63ce`, `#a479e2`, `#b694e8`, `#b99aff`, `#d0bcf1`, `#e3d7ff`, `#e4d7f5` |

---

## Syntax Options

### 1. Declarative Table Syntax (`label { ... }`)
```lua
return label {
    id = "lbl_receipts",
    name = "Receipts",
    message_list_visibility = "show",
    label_list_visibility = "labelShow",
    color = {
        text = "white",
        background = "#43d692"
    }
}
```

### 2. Fluent LabelBuilder Syntax (`label():...:build()`)
```lua
return label()
    :id("lbl_sec")
    :name("Security-Alerts")
    :show_in_message_list()
    :show_if_unread()
    :color("white", "#fb4c2f")
    :build()
```

### 3. Color Helper Function (`color(text, background)`)
```lua
return label {
    name = "Engineering",
    message_list_visibility = "hide",
    label_list_visibility = "labelShow",
    color = color("black", "#c9daf8")
}
```

---

## JSON Serialization (`ToDictionary`)

Calling `label.ToDictionary()` formats directly into the official Google Gmail REST API schema:

```json
{
  "id": "lbl_receipts",
  "name": "Receipts",
  "messageListVisibility": "show",
  "labelListVisibility": "labelShow",
  "color": {
    "textColor": "#ffffff",
    "backgroundColor": "#43d692"
  }
}
```

---

## Validation Rules

1. **Label Name**:
   - Must not be empty or whitespace.
   - Cannot collide with reserved system labels (`INBOX`, `UNREAD`, `STARRED`, `TRASH`, `SPAM`, `DRAFT`, `SENT`, `IMPORTANT`, `CHAT`, `CATEGORY_*`).
2. **Visibility Options**:
   - `messageListVisibility` must be either `'show'` or `'hide'`.
   - `labelListVisibility` must be one of `'labelShow'`, `'labelShowIfUnread'`, `'labelHide'` (or supported aliases `'show'`, `'show_if_unread'`, `'hide'`).
3. **Color Pairing & Hex Validity**:
   - If color is specified, both `textColor` and `backgroundColor` are strictly required.
   - Both text and background colors must match one of the 102 predefined hex colors or supported aliases (`black`, `white`).

---

## Label Diffing & Dry-Run Engine

VitaCernita provides a powerful diff engine via [`GmailLabelDiffer`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Labels/Diff/GmailLabelDiffer.cs) to compare desired label definitions (e.g. from Lua configuration) against existing labels in a Gmail account (e.g. from the Gmail API).

The diff engine is decoupled and serves two primary use cases:
1. **Programmatic Synchronization**: Generates exact API command payloads (`GetCreatePayload()`, `GetPatchPayload()`, `GetDeleteId()`) for Gmail REST API calls.
2. **Dry-Run Reporting**: Produces structured, human-readable reports displaying additions, deletions, and field-level modifications before applying any changes.

### Comparison Models & Objects

- [`GmailLabelDiffer`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Labels/Diff/GmailLabelDiffer.cs): Static methods `Diff(...)`, `DiffSets(...)`, `DiffApiListResponse(...)`, and `DiffJson(...)`.
- [`LabelDiff`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Labels/Diff/LabelDiff.cs): Difference for an individual label (`DiffType`: `Unchanged`, `Added`, `Removed`, `Modified`).
- [`LabelFieldDiff`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Labels/Diff/LabelFieldDiff.cs): Change details for a specific field (`FieldName`, `CurrentValue`, `DesiredValue`).
- [`LabelSetDiff`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Labels/Diff/LabelSetDiff.cs): Aggregated diff across label collections with `Creations`, `Deletions`, `Modifications`, and `Unchanged`.
- [`LabelDiffOptions`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Labels/Diff/LabelDiffOptions.cs): Options controlling matching strategies and selective field filtering.

### Diff Options & Selective Comparison

[`LabelDiffOptions`](file:///home/zimbu/Work/VitaCernita/src/VitaCernita.Core/Labels/Diff/LabelDiffOptions.cs) provides fine-grained control:

| Option | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `MatchBy` | `LabelMatchKey` | `Name` | Strategy used to pair labels: `Name`, `Id`, or `IdThenName`. |
| `CaseInsensitiveNameMatch` | `bool` | `true` | When matching by name, whether casing differences are ignored. |
| `IgnoreUnsetDesiredFields` | `bool` | `false` | When `true`, fields unset (null) in the desired configuration are ignored rather than treated as deletions/resets. |
| `FieldsToCompare` | `IReadOnlySet<string>?` | `null` | Optional whitelist of fields to compare (e.g. only compare visibility or color). |
| `FieldsToIgnore` | `IReadOnlySet<string>?` | `null` | Optional blacklist of fields to ignore during comparison. |
| `IncludeUnchanged` | `bool` | `true` | Whether unchanged labels should be retained in the `Differences` collection. |

### Example 1: Diffing Against Gmail API `users.labels.list` Response

```csharp
using VitaCernita.Core.Labels;
using VitaCernita.Core.Labels.Diff;

// Load desired labels from Lua configuration
var loader = new GmailLabelLoader();
List<GmailLabel> desiredLabels = await loader.LoadLabelsFromFileAsync("labels.lua");

// Fetch labels JSON from Gmail API (users.labels.list)
string apiJsonResponse = await gmailClient.ListLabelsRawJsonAsync();

// Compute diff (automatically filters out system labels like INBOX, SENT, TRASH)
LabelSetDiff diff = GmailLabelDiffer.DiffApiListResponse(apiJsonResponse, desiredLabels);

// Display dry-run summary & report
Console.WriteLine(diff.ToSummaryString());
Console.WriteLine(diff.ToDryRunReport());
```

### Example 2: Programmatic Execution / Synchronization

```csharp
// 1. Create newly added labels
foreach (var creation in diff.Creations)
{
    Dictionary<string, object> payload = creation.GetCreatePayload()!;
    await gmailClient.CreateLabelAsync(payload);
}

// 2. Patch modified labels with only the changed fields
foreach (var modification in diff.Modifications)
{
    string labelId = modification.Id!;
    Dictionary<string, object> patchPayload = modification.GetPatchPayload();
    await gmailClient.PatchLabelAsync(labelId, patchPayload);
}

// 3. Delete removed labels (if deletion sync is desired)
foreach (var deletion in diff.Deletions)
{
    string labelId = deletion.GetDeleteId()!;
    await gmailClient.DeleteLabelAsync(labelId);
}
```

### Dry-Run Output Sample

```text
======================================================================
VitaCernita Label Diff Report (Dry Run)
======================================================================
Summary: 1 to create, 1 to update, 1 to delete, 2 unchanged.

[+] Create (1):
  + 'BrandNew' (MessageList: show, Color: [#ffffff / #43d692])

[~] Update (1):
  ~ 'Updates' (ID: Label_22):
      * messageListVisibility: show -> hide
      * color: [#000000 / #ffffff] -> [#ffffff / #000000]

[-] Delete (1):
  - 'DeprecatedTag' (ID: Label_99)

[=] Unchanged (2):
  = 'Receipts' (ID: Label_10)
  = 'Work' (ID: Label_11)
======================================================================
```
