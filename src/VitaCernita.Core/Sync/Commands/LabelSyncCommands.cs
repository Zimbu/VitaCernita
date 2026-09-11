using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Labels.Diff;

namespace VitaCernita.Core.Sync.Commands;

/// <summary>
/// Command to create a new Gmail label in the target account.
/// </summary>
public sealed class CreateLabelCommand : SyncCommandBase
{
    public GmailLabel Label { get; }
    public string LabelName => TargetIdentifier;

    public CreateLabelCommand(GmailLabel label, string? commandId = null)
        : base(
            commandId,
            SyncResourceType.Label,
            SyncActionType.Create,
            targetIdentifier: label?.Name ?? throw new ArgumentNullException(nameof(label)),
            targetId: null,
            description: FormatDescription(label),
            detailedDescription: FormatDetailedDescription(label),
            payload: label.ToDictionary())
    {
        Label = label;
    }

    private static string FormatDescription(GmailLabel label)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(label.MessageListVisibility))
            details.Add($"MessageList: {label.MessageListVisibility}");
        if (!string.IsNullOrWhiteSpace(label.LabelListVisibility))
            details.Add($"LabelList: {label.LabelListVisibility}");
        if (label.Color != null)
            details.Add($"Color: [{label.Color.TextColor} / {label.Color.BackgroundColor}]");

        string extra = details.Count > 0 ? $" ({string.Join(", ", details)})" : "";
        return $"Create Label '{label.Name}'{extra}";
    }

    private static string FormatDetailedDescription(GmailLabel label)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Create Label: '{label.Name}'");
        if (label.MessageListVisibility != null) sb.AppendLine($"  MessageListVisibility: {label.MessageListVisibility}");
        if (label.LabelListVisibility != null) sb.AppendLine($"  LabelListVisibility: {label.LabelListVisibility}");
        if (label.Color != null) sb.AppendLine($"  Color: Text={label.Color.TextColor}, Bg={label.Color.BackgroundColor}");
        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// Command to update (patch) an existing Gmail label in the target account.
/// </summary>
public sealed class UpdateLabelCommand : SyncCommandBase
{
    public string LabelId => TargetId!;
    public string LabelName => TargetIdentifier;
    public GmailLabel CurrentLabel { get; }
    public GmailLabel DesiredLabel { get; }
    public IReadOnlyList<LabelFieldDiff> FieldDifferences { get; }

    public UpdateLabelCommand(
        string labelId,
        GmailLabel currentLabel,
        GmailLabel desiredLabel,
        IReadOnlyList<LabelFieldDiff> fieldDifferences,
        Dictionary<string, object>? patchPayload = null,
        string? commandId = null)
        : base(
            commandId,
            SyncResourceType.Label,
            SyncActionType.Update,
            targetIdentifier: (desiredLabel ?? throw new ArgumentNullException(nameof(desiredLabel))).Name,
            targetId: labelId ?? throw new ArgumentNullException(nameof(labelId)),
            description: $"Update Label '{desiredLabel.Name}' (ID: {labelId}): {fieldDifferences?.Count ?? 0} change(s)",
            detailedDescription: FormatDetailedDescription(labelId, desiredLabel.Name, fieldDifferences),
            payload: patchPayload)
    {
        CurrentLabel = currentLabel ?? throw new ArgumentNullException(nameof(currentLabel));
        DesiredLabel = desiredLabel;
        FieldDifferences = fieldDifferences ?? Array.Empty<LabelFieldDiff>();
    }

    public static UpdateLabelCommand FromDiff(LabelDiff diff, string? commandId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string id = diff.Id ?? diff.CurrentLabel?.Id ?? throw new InvalidOperationException($"Cannot update label '{diff.Name}' without a valid ID.");
        return new UpdateLabelCommand(
            id,
            diff.CurrentLabel ?? new GmailLabel(diff.Name),
            diff.DesiredLabel ?? throw new InvalidOperationException($"Cannot update label '{diff.Name}' without a desired specification."),
            diff.FieldDifferences,
            diff.GetPatchPayload(),
            commandId);
    }

    private static string FormatDetailedDescription(string labelId, string name, IReadOnlyList<LabelFieldDiff>? diffs)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Update Label: '{name}' (ID: {labelId})");
        if (diffs != null)
        {
            foreach (var d in diffs)
            {
                sb.AppendLine($"  ~ {d}");
            }
        }
        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// Command to delete an obsolete Gmail label from the target account.
/// </summary>
public sealed class DeleteLabelCommand : SyncCommandBase
{
    public string LabelId => TargetId!;
    public string LabelName => TargetIdentifier;
    public GmailLabel? CurrentLabel { get; }

    public DeleteLabelCommand(string labelId, string labelName, GmailLabel? currentLabel = null, string? commandId = null)
        : base(
            commandId,
            SyncResourceType.Label,
            SyncActionType.Delete,
            targetIdentifier: labelName ?? throw new ArgumentNullException(nameof(labelName)),
            targetId: labelId ?? throw new ArgumentNullException(nameof(labelId)),
            description: $"Delete Label '{labelName}' (ID: {labelId})",
            detailedDescription: $"Delete Label: '{labelName}' (ID: {labelId})",
            payload: null)
    {
        CurrentLabel = currentLabel;
    }

    public static DeleteLabelCommand FromDiff(LabelDiff diff, string? commandId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string id = diff.GetDeleteId() ?? throw new InvalidOperationException($"Cannot delete label '{diff.Name}' without a valid ID.");
        return new DeleteLabelCommand(id, diff.Name, diff.CurrentLabel, commandId);
    }
}
