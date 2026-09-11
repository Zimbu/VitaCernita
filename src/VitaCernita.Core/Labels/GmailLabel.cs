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
        LabelColor? color = null)
    {
        Name = LabelValidator.ValidateName(name);
        Id = id;
        MessageListVisibility = LabelValidator.ValidateMessageListVisibility(messageListVisibility);
        LabelListVisibility = LabelValidator.ValidateLabelListVisibility(labelListVisibility);
        Color = color;
    }

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
}
