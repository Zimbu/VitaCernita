using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Sync;

namespace VitaCernita.Core.Operations.Filters;

/// <summary>
/// Gmail API operation to update an altered Gmail filter identified by ID to match the desired configuration.
/// Because Gmail REST API does not provide a direct PATCH endpoint for filters, executing an update
/// involves deleting the target filter ID and creating the replacement filter.
/// </summary>
public class UpdateFilterOperation : GmailOperationBase
{
    public string FilterId => TargetId!;
    public GmailFilter CurrentFilter { get; }
    public GmailFilter DesiredFilter { get; }
    public IReadOnlyList<FieldDiff> FieldDifferences { get; }

    public UpdateFilterOperation(
        string filterId,
        GmailFilter currentFilter,
        GmailFilter desiredFilter,
        IReadOnlyList<FieldDiff>? fieldDifferences,
        string? operationId = null,
        IEnumerable<GmailLabel>? knownLabels = null)
        : base(
            operationId,
            SyncResourceType.Filter,
            SyncActionType.Update,
            targetIdentifier: !string.IsNullOrWhiteSpace(desiredFilter?.Name) ? desiredFilter.Name : filterId,
            targetId: filterId ?? throw new ArgumentNullException(nameof(filterId)),
            description: FormatDescription(filterId, desiredFilter, fieldDifferences),
            detailedDescription: FormatDetailedDescription(filterId, desiredFilter, currentFilter, fieldDifferences),
            payload: desiredFilter?.ToDictionary(knownLabels))
    {
        CurrentFilter = currentFilter ?? throw new ArgumentNullException(nameof(currentFilter));
        DesiredFilter = desiredFilter ?? throw new ArgumentNullException(nameof(desiredFilter));
        FieldDifferences = fieldDifferences ?? Array.Empty<FieldDiff>();
    }

    private static string FormatDescription(
        string filterId,
        GmailFilter? desired,
        IReadOnlyList<FieldDiff>? differences)
    {
        string nameInfo = !string.IsNullOrWhiteSpace(desired?.Name) ? $" '{desired.Name}'" : "";
        int count = differences?.Count ?? 0;
        return $"Update Filter{nameInfo} (ID: {filterId}): {count} change(s)";
    }

    private static string FormatDetailedDescription(
        string filterId,
        GmailFilter? desired,
        GmailFilter? current,
        IReadOnlyList<FieldDiff>? differences)
    {
        var sb = new StringBuilder();
        string nameInfo = !string.IsNullOrWhiteSpace(desired?.Name) ? $"Name: '{desired.Name}'" : "Filter";
        sb.AppendLine($"Update {nameInfo} (ID: {filterId})");
        sb.AppendLine("  Note: Gmail requires re-creating altered filters (Delete ID then Create new).");

        if (differences != null && differences.Count > 0)
        {
            sb.AppendLine("  Changes:");
            foreach (var diff in differences)
            {
                sb.AppendLine($"    ~ {diff.FieldName}: '{diff.CurrentValue ?? "<unset>"}' -> '{diff.DesiredValue ?? "<unset>"}'");
            }
        }

        if (current != null)
        {
            sb.AppendLine($"  Current Query : {current.ToGmailQuery()}");
            sb.AppendLine($"  Current Action: {current.Action?.ToString() ?? "<none>"}");
        }

        if (desired != null)
        {
            sb.AppendLine($"  Desired Query : {desired.ToGmailQuery()}");
            sb.AppendLine($"  Desired Action: {desired.Action?.ToString() ?? "<none>"}");
        }

        return sb.ToString().TrimEnd();
    }
}
