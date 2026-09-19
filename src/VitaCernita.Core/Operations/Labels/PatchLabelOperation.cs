using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Sync;

namespace VitaCernita.Core.Operations.Labels;

/// <summary>
/// Gmail API operation to update / patch an existing label via PATCH users.labels.
/// </summary>
public class PatchLabelOperation : GmailOperationBase
{
    public string LabelId => TargetId!;
    public string LabelName => TargetIdentifier;
    public GmailLabel CurrentLabel { get; }
    public GmailLabel DesiredLabel { get; }
    public IReadOnlyList<FieldDiff> FieldDifferences { get; }

    public PatchLabelOperation(
        string labelId,
        GmailLabel currentLabel,
        GmailLabel desiredLabel,
        IReadOnlyList<FieldDiff>? fieldDifferences,
        Dictionary<string, object>? patchPayload = null,
        string? operationId = null)
        : base(
            operationId,
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
        FieldDifferences = fieldDifferences ?? Array.Empty<FieldDiff>();
    }

    private static string FormatDetailedDescription(
        string labelId,
        string labelName,
        IReadOnlyList<FieldDiff>? differences)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Update Label: '{labelName}' (ID: {labelId})");
        if (differences != null && differences.Count > 0)
        {
            sb.AppendLine("  Changes:");
            foreach (var diff in differences)
            {
                sb.AppendLine($"    ~ {diff.FieldName}: '{diff.CurrentValue ?? "<unset>"}' -> '{diff.DesiredValue ?? "<unset>"}'");
            }
        }
        return sb.ToString().TrimEnd();
    }
}
