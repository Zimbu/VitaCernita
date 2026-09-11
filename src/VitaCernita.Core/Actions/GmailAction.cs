using System;
using System.Collections.Generic;
using System.Linq;
using VitaCernita.Core.Actions.Validation;

namespace VitaCernita.Core.Actions;

/// <summary>
/// Represents actions to perform on messages matching a filter in the Gmail API.
/// Maps directly to Google Gmail API users.settings.filters Action object:
/// { addLabelIds = [...], removeLabelIds = [...], forward = "..." }
/// </summary>
public sealed class GmailAction : IEquatable<GmailAction>
{
    public HashSet<string> AddLabelIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> RemoveLabelIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Forward { get; set; }

    public bool IsEmpty => AddLabelIds.Count == 0 && RemoveLabelIds.Count == 0 && string.IsNullOrWhiteSpace(Forward);

    public bool IsArchive => RemoveLabelIds.Contains(SystemLabels.Inbox);
    public bool IsMarkUnread => RemoveLabelIds.Contains(SystemLabels.Unread);
    public bool IsStarred => AddLabelIds.Contains(SystemLabels.Starred);
    public bool IsDelete => AddLabelIds.Contains(SystemLabels.Trash);
    public bool IsImportant => AddLabelIds.Contains(SystemLabels.Important);

    public string? Category => AddLabelIds.FirstOrDefault(l => l.StartsWith("CATEGORY_", StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> CustomLabels =>
        AddLabelIds.Where(l => !SystemLabels.IsSystemLabel(l))
                   .OrderBy(l => l, StringComparer.OrdinalIgnoreCase)
                   .ToList();

    public GmailAction Archive()
    {
        RemoveLabelIds.Add(SystemLabels.Inbox);
        return this;
    }

    public GmailAction MarkUnread()
    {
        RemoveLabelIds.Add(SystemLabels.Unread);
        return this;
    }

    public GmailAction Star()
    {
        AddLabelIds.Add(SystemLabels.Starred);
        return this;
    }

    public GmailAction Delete()
    {
        AddLabelIds.Add(SystemLabels.Trash);
        return this;
    }

    public GmailAction MarkImportant()
    {
        AddLabelIds.Add(SystemLabels.Important);
        return this;
    }

    public GmailAction AddCategory(string category)
    {
        string labelId = ActionValidator.ValidateAndNormalizeCategory(category);
        AddLabelIds.Add(labelId);
        return this;
    }

    public GmailAction AddCustomLabel(string label)
    {
        string valid = ActionValidator.ValidateLabel(label);
        AddLabelIds.Add(valid);
        return this;
    }

    public GmailAction SetForward(string email)
    {
        Forward = ActionValidator.ValidateForwardEmail(email);
        return this;
    }

    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>();
        if (AddLabelIds.Count > 0)
        {
            dict["addLabelIds"] = AddLabelIds.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        }
        if (RemoveLabelIds.Count > 0)
        {
            dict["removeLabelIds"] = RemoveLabelIds.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        }
        if (!string.IsNullOrWhiteSpace(Forward))
        {
            dict["forward"] = Forward;
        }
        return dict;
    }

    public bool Equals(GmailAction? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return AddLabelIds.SetEquals(other.AddLabelIds) &&
               RemoveLabelIds.SetEquals(other.RemoveLabelIds) &&
               string.Equals(Forward, other.Forward, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => obj is GmailAction other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var id in AddLabelIds.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            hash.Add(id, StringComparer.OrdinalIgnoreCase);
        foreach (var id in RemoveLabelIds.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            hash.Add(id, StringComparer.OrdinalIgnoreCase);
        if (Forward != null)
            hash.Add(Forward, StringComparer.OrdinalIgnoreCase);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (IsArchive) parts.Add("archive");
        if (IsMarkUnread) parts.Add("mark_unread");
        if (IsStarred) parts.Add("star");
        if (IsDelete) parts.Add("delete");
        if (IsImportant) parts.Add("mark_important");
        if (Category != null) parts.Add($"category:{Category}");
        foreach (var cl in CustomLabels) parts.Add($"label:{cl}");
        if (Forward != null) parts.Add($"forward:{Forward}");
        return parts.Count > 0 ? string.Join(", ", parts) : "(empty action)";
    }
}
