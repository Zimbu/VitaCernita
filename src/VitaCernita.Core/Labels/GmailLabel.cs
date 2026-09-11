using System;
using System.Collections.Generic;
using VitaCernita.Core.Labels.Validation;

namespace VitaCernita.Core.Labels;

/// <summary>
/// Represents a user label in Gmail matching the Google Workspace Gmail API users.labels resource.
/// </summary>
public class GmailLabel : IEquatable<GmailLabel>
{
    public string? Id { get; set; }
    public string Name { get; set; }
    public string? MessageListVisibility { get; set; }
    public string? LabelListVisibility { get; set; }
    public LabelColor? Color { get; set; }

    public GmailLabel(
        string name,
        string? id = null,
        string? messageListVisibility = null,
        string? labelListVisibility = null,
        LabelColor? color = null,
        bool isSystemLabel = false)
    {
        Name = isSystemLabel ? (name ?? string.Empty).Trim() : LabelValidator.ValidateName(name);
        Id = id;
        MessageListVisibility = LabelValidator.ValidateMessageListVisibility(messageListVisibility);
        LabelListVisibility = LabelValidator.ValidateLabelListVisibility(labelListVisibility);
        Color = color;
    }

    /// <summary>
    /// Factory for creating system label representations (e.g. from the Gmail API).
    /// </summary>
    public static GmailLabel CreateSystemLabel(string name, string? id = null) =>
        new(name, id, isSystemLabel: true);

    /// <summary>
    /// Serializes this label to a dictionary conforming to the Gmail API users.labels resource.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(Id))
        {
            dict["id"] = Id;
        }

        dict["name"] = Name;

        if (!string.IsNullOrWhiteSpace(MessageListVisibility))
        {
            dict["messageListVisibility"] = MessageListVisibility;
        }

        if (!string.IsNullOrWhiteSpace(LabelListVisibility))
        {
            dict["labelListVisibility"] = LabelListVisibility;
        }

        if (Color != null)
        {
            dict["color"] = Color.ToDictionary();
        }

        return dict;
    }

    public override string ToString()
    {
        var parts = new List<string> { $"Name: '{Name}'" };
        if (!string.IsNullOrWhiteSpace(Id)) parts.Add($"Id: '{Id}'");
        if (!string.IsNullOrWhiteSpace(MessageListVisibility)) parts.Add($"MessageList: {MessageListVisibility}");
        if (!string.IsNullOrWhiteSpace(LabelListVisibility)) parts.Add($"LabelList: {LabelListVisibility}");
        if (Color != null) parts.Add($"Color: [{Color}]");
        return $"Label({string.Join(", ", parts)})";
    }

    public bool Equals(GmailLabel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id, StringComparison.Ordinal) &&
               string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               string.Equals(MessageListVisibility, other.MessageListVisibility, StringComparison.Ordinal) &&
               string.Equals(LabelListVisibility, other.LabelListVisibility, StringComparison.Ordinal) &&
               Equals(Color, other.Color);
    }

    public override bool Equals(object? obj) => obj is GmailLabel other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, Name, MessageListVisibility, LabelListVisibility, Color);

    /// <summary>
    /// Constructs a GmailLabel instance from a dictionary representing a Gmail API label resource.
    /// </summary>
    public static GmailLabel FromDictionary(IReadOnlyDictionary<string, object?> dict)
    {
        if (dict == null) throw new ArgumentNullException(nameof(dict));

        string? id = GetString(dict, "id", "Id");
        string? name = GetString(dict, "name", "Name", "displayName", "display_name");
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new LabelValidationException("Label name cannot be empty.");
        }

        string? mlv = GetString(dict, "messageListVisibility", "message_list_visibility", "MessageListVisibility");
        string? llv = GetString(dict, "labelListVisibility", "label_list_visibility", "LabelListVisibility");

        LabelColor? color = null;
        if (dict.TryGetValue("color", out var colorVal) && colorVal != null)
        {
            color = ParseColorValue(colorVal);
        }

        return new GmailLabel(name, id, mlv, llv, color);
    }

    /// <summary>
    /// Parses a single Gmail label JSON string into a GmailLabel instance.
    /// </summary>
    public static GmailLabel FromJson(string json)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return FromJsonElement(doc.RootElement);
    }

    /// <summary>
    /// Parses a JsonElement representing a Gmail API label resource.
    /// </summary>
    public static GmailLabel FromJsonElement(System.Text.Json.JsonElement element)
    {
        string? id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        string? name = element.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new LabelValidationException("Label name cannot be empty in JSON.");
        }

        string? mlv = element.TryGetProperty("messageListVisibility", out var mlvProp) ? mlvProp.GetString() : null;
        string? llv = element.TryGetProperty("labelListVisibility", out var llvProp) ? llvProp.GetString() : null;

        LabelColor? color = null;
        if (element.TryGetProperty("color", out var colorProp) && colorProp.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            string? text = colorProp.TryGetProperty("textColor", out var tc) ? tc.GetString() : null;
            string? bg = colorProp.TryGetProperty("backgroundColor", out var bc) ? bc.GetString() : null;
            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(bg))
            {
                color = new LabelColor(text, bg);
            }
        }

        bool isSystem = false;
        if (element.TryGetProperty("type", out var typeProp) &&
            string.Equals(typeProp.GetString(), "system", StringComparison.OrdinalIgnoreCase))
        {
            isSystem = true;
        }
        else if (LabelValidator.ReservedSystemLabels.Contains(name))
        {
            isSystem = true;
        }

        return new GmailLabel(name, id, mlv, llv, color, isSystemLabel: isSystem);
    }

    /// <summary>
    /// Parses the JSON response from Gmail API users.labels.list into a list of user GmailLabel instances.
    /// </summary>
    public static List<GmailLabel> FromApiListResponse(string json, bool onlyUserLabels = true)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var labels = new List<GmailLabel>();

        System.Text.Json.JsonElement arrayElement;
        if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            arrayElement = doc.RootElement;
        }
        else if (doc.RootElement.TryGetProperty("labels", out var lProp) && lProp.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            arrayElement = lProp;
        }
        else
        {
            return labels;
        }

        foreach (var item in arrayElement.EnumerateArray())
        {
            if (onlyUserLabels)
            {
                if (item.TryGetProperty("type", out var typeProp) &&
                    string.Equals(typeProp.GetString(), "system", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (item.TryGetProperty("name", out var nameProp))
                {
                    string? n = nameProp.GetString();
                    if (!string.IsNullOrWhiteSpace(n) && LabelValidator.ReservedSystemLabels.Contains(n))
                    {
                        continue;
                    }
                }
            }

            labels.Add(FromJsonElement(item));
        }

        return labels;
    }

    private static string? GetString(IReadOnlyDictionary<string, object?> dict, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (val is System.Text.Json.JsonElement je)
                {
                    return je.ValueKind == System.Text.Json.JsonValueKind.String ? je.GetString() : je.ToString();
                }
                return val.ToString();
            }
        }
        return null;
    }

    private static LabelColor? ParseColorValue(object colorVal)
    {
        if (colorVal is LabelColor lc) return lc;

        if (colorVal is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            string? text = je.TryGetProperty("textColor", out var tc) ? tc.GetString() : null;
            string? bg = je.TryGetProperty("backgroundColor", out var bc) ? bc.GetString() : null;
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(bg))
            {
                throw new LabelValidationException("Both textColor and backgroundColor must be provided when setting a label color.");
            }
            return new LabelColor(text, bg);
        }

        if (colorVal is IReadOnlyDictionary<string, object?> cd)
        {
            string? text = GetString(cd, "textColor", "text_color", "text");
            string? bg = GetString(cd, "backgroundColor", "background_color", "background", "bg");
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(bg))
            {
                throw new LabelValidationException("Both textColor and backgroundColor must be provided when setting a label color.");
            }
            return new LabelColor(text!, bg!);
        }

        return null;
    }
}
