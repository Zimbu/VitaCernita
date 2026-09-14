using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Filters.Diff;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Sync.Commands;

/// <summary>
/// Command to create a new search filter in the target Gmail account.
/// Maps to POST users.settings.filters.
/// </summary>
public sealed class CreateFilterCommand : SyncCommandBase
{
    public GmailFilter Filter { get; }
    public string? FilterName => Filter.Name;

    public CreateFilterCommand(GmailFilter filter, string? commandId = null, IEnumerable<GmailLabel>? knownLabels = null)
        : base(
            commandId,
            SyncResourceType.Filter,
            SyncActionType.Create,
            targetIdentifier: !string.IsNullOrWhiteSpace(filter?.Name) ? filter.Name : (filter?.ToGmailQuery() ?? "<filter>"),
            targetId: null,
            description: FormatDescription(filter!),
            detailedDescription: FormatDetailedDescription(filter!),
            payload: filter?.ToDictionary(knownLabels))
    {
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    public static CreateFilterCommand FromDiff(FilterDiff diff, string? commandId = null, IEnumerable<GmailLabel>? knownLabels = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        if (diff.DesiredFilter == null) throw new InvalidOperationException("Cannot create filter without a desired filter specification.");
        return new CreateFilterCommand(diff.DesiredFilter, commandId, knownLabels);
    }

    private static string FormatDescription(GmailFilter filter)
    {
        string nameInfo = !string.IsNullOrWhiteSpace(filter.Name) ? $" '{filter.Name}'" : "";
        string queryStr = filter.ToGmailQuery();
        string actionStr = filter.Action?.ToString() ?? "<none>";
        return $"Create Filter{nameInfo}: query '{queryStr}' -> action {actionStr}";
    }

    private static string FormatDetailedDescription(GmailFilter filter)
    {
        var sb = new StringBuilder();
        string nameInfo = !string.IsNullOrWhiteSpace(filter.Name) ? $"Name: '{filter.Name}'" : "Filter";
        sb.AppendLine($"Create {nameInfo}");
        sb.AppendLine($"  Query : {filter.ToGmailQuery()}");
        sb.AppendLine($"  Action: {filter.Action?.ToString() ?? "<none>"}");
        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// Command to update / edit an altered Gmail filter identified by ID to match the desired configuration.
/// Note: Because the Gmail REST API does not provide a direct PATCH endpoint for filters, executing
/// an update involves deleting the target filter ID and creating the replacement filter.
/// </summary>
public sealed class UpdateFilterCommand : SyncCommandBase
{
    public string FilterId => TargetId!;
    public GmailFilter CurrentFilter { get; }
    public GmailFilter DesiredFilter { get; }
    public IReadOnlyList<FilterFieldDiff> FieldDifferences { get; }

    public UpdateFilterCommand(
        string filterId,
        GmailFilter currentFilter,
        GmailFilter desiredFilter,
        IReadOnlyList<FilterFieldDiff> fieldDifferences,
        string? commandId = null,
        IEnumerable<GmailLabel>? knownLabels = null)
        : base(
            commandId,
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
        FieldDifferences = fieldDifferences ?? Array.Empty<FilterFieldDiff>();
    }

    public static UpdateFilterCommand FromDiff(FilterDiff diff, string? commandId = null, IEnumerable<GmailLabel>? knownLabels = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string id = diff.Id ?? diff.CurrentFilter?.Id ?? throw new InvalidOperationException("Cannot update filter without a valid ID.");
        if (diff.CurrentFilter == null) throw new InvalidOperationException("Cannot update filter without current filter state.");
        if (diff.DesiredFilter == null) throw new InvalidOperationException("Cannot update filter without desired filter state.");

        return new UpdateFilterCommand(id, diff.CurrentFilter, diff.DesiredFilter, diff.FieldDifferences, commandId, knownLabels);
    }

    private static string FormatDescription(string filterId, GmailFilter? desired, IReadOnlyList<FilterFieldDiff>? diffs)
    {
        string nameInfo = !string.IsNullOrWhiteSpace(desired?.Name) ? $" '{desired.Name}'" : "";
        int count = diffs?.Count ?? 0;
        return $"Update Filter{nameInfo} (ID: {filterId}): {count} change(s)";
    }

    private static string FormatDetailedDescription(
        string filterId,
        GmailFilter? desired,
        GmailFilter? current,
        IReadOnlyList<FilterFieldDiff>? diffs)
    {
        var sb = new StringBuilder();
        string nameInfo = !string.IsNullOrWhiteSpace(desired?.Name) ? $" '{desired.Name}'" : "";
        sb.AppendLine($"Update Filter{nameInfo} (ID: {filterId}):");
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
/// Command to delete an obsolete filter from the target Gmail account.
/// Maps to DELETE users.settings.filters/{id}.
/// </summary>
public sealed class DeleteFilterCommand : SyncCommandBase
{
    public string FilterId => TargetId!;
    public GmailFilter? CurrentFilter { get; }

    public DeleteFilterCommand(string filterId, GmailFilter? currentFilter = null, string? commandId = null)
        : base(
            commandId,
            SyncResourceType.Filter,
            SyncActionType.Delete,
            targetIdentifier: !string.IsNullOrWhiteSpace(currentFilter?.Name) ? currentFilter.Name : filterId,
            targetId: filterId ?? throw new ArgumentNullException(nameof(filterId)),
            description: FormatDescription(filterId, currentFilter),
            detailedDescription: FormatDetailedDescription(filterId, currentFilter),
            payload: null)
    {
        CurrentFilter = currentFilter;
    }

    public static DeleteFilterCommand FromDiff(FilterDiff diff, string? commandId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string id = diff.GetDeleteId() ?? throw new InvalidOperationException("Cannot delete filter without a valid ID.");
        return new DeleteFilterCommand(id, diff.CurrentFilter, commandId);
    }

    private static string FormatDescription(string filterId, GmailFilter? current)
    {
        string nameInfo = !string.IsNullOrWhiteSpace(current?.Name) ? $" '{current.Name}'" : "";
        string queryInfo = current != null ? $": query '{current.ToGmailQuery()}'" : "";
        return $"Delete Filter{nameInfo} (ID: {filterId}){queryInfo}";
    }

    private static string FormatDetailedDescription(string filterId, GmailFilter? current)
    {
        var sb = new StringBuilder();
        string nameInfo = !string.IsNullOrWhiteSpace(current?.Name) ? $" '{current.Name}'" : "";
        sb.AppendLine($"Delete Filter{nameInfo} (ID: {filterId})");
        if (current != null)
        {
            sb.AppendLine($"  Query : {current.ToGmailQuery()}");
            sb.AppendLine($"  Action: {current.Action?.ToString() ?? "<none>"}");
        }
        return sb.ToString().TrimEnd();
    }
}
