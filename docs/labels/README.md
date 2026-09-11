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
