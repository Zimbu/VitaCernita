using System;
using System.Collections.Generic;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Operations.Labels;

namespace VitaCernita.Core.Operations.Translators;

/// <summary>
/// Translates a pure model ResourceDiff&lt;GmailLabel&gt; into executable Gmail REST API operations
/// and constructs Gmail-specific users.labels API payloads.
/// </summary>
public static class LabelOperationTranslator
{
    /// <summary>
    /// Generates a JSON-ready dictionary payload for a Gmail API users.labels.patch call,
    /// containing only the fields that actually changed.
    /// </summary>
    public static Dictionary<string, object> BuildPatchPayload(ResourceDiff<GmailLabel> diff)
    {
        var payload = new Dictionary<string, object>();
        if (diff?.Desired == null) return payload;

        foreach (var fieldDiff in diff.FieldDifferences)
        {
            switch (fieldDiff.FieldName)
            {
                case "name":
                    payload["name"] = diff.Desired.Name;
                    break;
                case "messageListVisibility":
                    if (diff.Desired.MessageListVisibility != null)
                    {
                        payload["messageListVisibility"] = diff.Desired.MessageListVisibility;
                    }
                    break;
                case "labelListVisibility":
                    if (diff.Desired.LabelListVisibility != null)
                    {
                        payload["labelListVisibility"] = diff.Desired.LabelListVisibility;
                    }
                    break;
                case "color":
                    if (diff.Desired.Color != null)
                    {
                        payload["color"] = new Dictionary<string, object>
                        {
                            ["textColor"] = diff.Desired.Color.TextColor,
                            ["backgroundColor"] = diff.Desired.Color.BackgroundColor
                        };
                    }
                    break;
                case "textColor":
                case "color.textColor":
                    EnsureColorDict(payload)["textColor"] = diff.Desired.Color?.TextColor ?? "";
                    break;
                case "backgroundColor":
                case "color.backgroundColor":
                    EnsureColorDict(payload)["backgroundColor"] = diff.Desired.Color?.BackgroundColor ?? "";
                    break;
            }
        }

        return payload;
    }

    private static Dictionary<string, object> EnsureColorDict(Dictionary<string, object> payload)
    {
        if (!payload.TryGetValue("color", out var existing) || existing is not Dictionary<string, object> colorDict)
        {
            colorDict = new Dictionary<string, object>();
            payload["color"] = colorDict;
        }
        return colorDict;
    }

    /// <summary>
    /// Generates a JSON-ready dictionary payload for a Gmail API users.labels.create call.
    /// </summary>
    public static Dictionary<string, object>? BuildCreatePayload(ResourceDiff<GmailLabel> diff)
    {
        return diff?.Desired?.ToDictionary();
    }

    /// <summary>
    /// Resolves the server ID of the label to be deleted in a Gmail API users.labels.delete call.
    /// </summary>
    public static string? ResolveDeleteId(ResourceDiff<GmailLabel> diff)
    {
        return diff?.Current?.Id ?? diff?.Id;
    }

    /// <summary>
    /// Translates a creation diff into a CreateLabelOperation.
    /// </summary>
    public static CreateLabelOperation ToCreateOperation(ResourceDiff<GmailLabel> diff, string? operationId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        if (diff.Desired == null) throw new InvalidOperationException("Cannot create label without a desired specification.");
        return new CreateLabelOperation(diff.Desired, operationId);
    }

    /// <summary>
    /// Translates a modification diff into a PatchLabelOperation.
    /// </summary>
    public static PatchLabelOperation ToPatchOperation(ResourceDiff<GmailLabel> diff, string? operationId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string name = diff.Identifier ?? diff.Desired?.Name ?? diff.Current?.Name ?? string.Empty;
        string id = diff.Id ?? diff.Current?.Id ?? throw new InvalidOperationException($"Cannot update label '{name}' without a valid ID.");
        return new PatchLabelOperation(
            id,
            diff.Current ?? new GmailLabel(name),
            diff.Desired ?? throw new InvalidOperationException($"Cannot update label '{name}' without a desired specification."),
            diff.FieldDifferences,
            BuildPatchPayload(diff),
            operationId);
    }

    /// <summary>
    /// Translates a deletion diff into a DeleteLabelOperation.
    /// </summary>
    public static DeleteLabelOperation ToDeleteOperation(ResourceDiff<GmailLabel> diff, string? operationId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string name = diff.Identifier ?? diff.Current?.Name ?? string.Empty;
        string id = ResolveDeleteId(diff) ?? throw new InvalidOperationException($"Cannot delete label '{name}' without a valid ID.");
        return new DeleteLabelOperation(id, name, operationId);
    }

    /// <summary>
    /// Translates any single ResourceDiff into its corresponding IGmailOperation (or null if unchanged).
    /// </summary>
    public static IGmailOperation? Translate(ResourceDiff<GmailLabel> diff, string? operationId = null)
    {
        return diff.DiffType switch
        {
            DiffKind.Added => ToCreateOperation(diff, operationId),
            DiffKind.Modified => ToPatchOperation(diff, operationId),
            DiffKind.Removed => ToDeleteOperation(diff, operationId),
            _ => null
        };
    }
}
