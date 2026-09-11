using System;
using System.Collections.Generic;
using System.Linq;

namespace VitaCernita.Core.Labels.Diff;

/// <summary>
/// Represents the difference between an existing Gmail label and a desired label specification.
/// </summary>
public sealed class LabelDiff
{
    public string Name { get; }
    public string? Id { get; }
    public LabelDiffType DiffType { get; }
    public GmailLabel? CurrentLabel { get; }
    public GmailLabel? DesiredLabel { get; }
    public IReadOnlyList<LabelFieldDiff> FieldDifferences { get; }

    public bool HasChanges => DiffType != LabelDiffType.Unchanged;

    public LabelDiff(
        string name,
        string? id,
        LabelDiffType diffType,
        GmailLabel? currentLabel,
        GmailLabel? desiredLabel,
        IReadOnlyList<LabelFieldDiff>? fieldDifferences = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Id = id;
        DiffType = diffType;
        CurrentLabel = currentLabel;
        DesiredLabel = desiredLabel;
        FieldDifferences = fieldDifferences ?? Array.Empty<LabelFieldDiff>();
    }

    /// <summary>
    /// Generates a JSON-ready dictionary payload for a Gmail API users.labels.patch call,
    /// containing only the fields that actually changed.
    /// </summary>
    public Dictionary<string, object> GetPatchPayload()
    {
        var payload = new Dictionary<string, object>();
        if (DesiredLabel == null) return payload;

        foreach (var diff in FieldDifferences)
        {
            switch (diff.FieldName)
            {
                case "name":
                    payload["name"] = DesiredLabel.Name;
                    break;
                case "messageListVisibility":
                    if (DesiredLabel.MessageListVisibility != null)
                    {
                        payload["messageListVisibility"] = DesiredLabel.MessageListVisibility;
                    }
                    break;
                case "labelListVisibility":
                    if (DesiredLabel.LabelListVisibility != null)
                    {
                        payload["labelListVisibility"] = DesiredLabel.LabelListVisibility;
                    }
                    break;
                case "color":
                    if (DesiredLabel.Color != null)
                    {
                        payload["color"] = DesiredLabel.Color.ToDictionary();
                    }
                    break;
            }
        }

        return payload;
    }

    /// <summary>
    /// Generates a JSON-ready dictionary payload for a Gmail API users.labels.create call.
    /// </summary>
    public Dictionary<string, object>? GetCreatePayload()
    {
        return DesiredLabel?.ToDictionary();
    }

    /// <summary>
    /// Returns the label ID to delete for a Gmail API users.labels.delete call.
    /// </summary>
    public string? GetDeleteId()
    {
        return CurrentLabel?.Id ?? Id;
    }

    public override string ToString()
    {
        return DiffType switch
        {
            LabelDiffType.Added => $"+ Label '{Name}' (Create)",
            LabelDiffType.Removed => $"- Label '{Name}' (Delete, ID: {Id ?? "<none>"})",
            LabelDiffType.Modified => $"~ Label '{Name}' ({FieldDifferences.Count} change(s): {string.Join(", ", FieldDifferences)})",
            _ => $"  Label '{Name}' (Unchanged)"
        };
    }
}
