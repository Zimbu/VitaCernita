using System;
using System.Collections.Generic;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Operations.Filters;
using VitaCernita.Core.Operations.Translators;

namespace VitaCernita.Core.Sync.Commands;

/// <summary>
/// Command to create a new search filter in the target Gmail account.
/// Maps to POST users.settings.filters.
/// </summary>
public sealed class CreateFilterCommand : CreateFilterOperation
{
    public CreateFilterCommand(GmailFilter filter, string? commandId = null, IEnumerable<GmailLabel>? knownLabels = null)
        : base(filter, commandId, knownLabels)
    {
    }

    public static CreateFilterCommand FromDiff(ResourceDiff<GmailFilter> diff, string? commandId = null, IEnumerable<GmailLabel>? knownLabels = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        if (diff.Desired == null) throw new InvalidOperationException("Cannot create filter without a desired filter specification.");
        return new CreateFilterCommand(diff.Desired, commandId, knownLabels);
    }
}

/// <summary>
/// Command to update / edit an altered Gmail filter identified by ID to match the desired configuration.
/// Note: Because the Gmail REST API does not provide a direct PATCH endpoint for filters, executing
/// an update involves deleting the target filter ID and creating the replacement filter.
/// </summary>
public sealed class UpdateFilterCommand : UpdateFilterOperation
{
    public UpdateFilterCommand(
        string filterId,
        GmailFilter currentFilter,
        GmailFilter desiredFilter,
        IReadOnlyList<FieldDiff> fieldDifferences,
        string? commandId = null,
        IEnumerable<GmailLabel>? knownLabels = null)
        : base(filterId, currentFilter, desiredFilter, fieldDifferences, commandId, knownLabels)
    {
    }

    public static UpdateFilterCommand FromDiff(ResourceDiff<GmailFilter> diff, string? commandId = null, IEnumerable<GmailLabel>? knownLabels = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string id = diff.Id ?? diff.Current?.Id ?? throw new InvalidOperationException("Cannot update filter without a valid ID.");
        if (diff.Current == null) throw new InvalidOperationException("Cannot update filter without current filter state.");
        if (diff.Desired == null) throw new InvalidOperationException("Cannot update filter without desired filter state.");

        return new UpdateFilterCommand(id, diff.Current, diff.Desired, diff.FieldDifferences, commandId, knownLabels);
    }
}

/// <summary>
/// Command to delete an obsolete filter from the target Gmail account.
/// Maps to DELETE users.settings.filters/{id}.
/// </summary>
public sealed class DeleteFilterCommand : DeleteFilterOperation
{
    public GmailFilter? CurrentFilter { get; }

    public DeleteFilterCommand(string filterId, GmailFilter? currentFilter = null, string? commandId = null)
        : base(filterId, currentFilter?.Name, commandId)
    {
        CurrentFilter = currentFilter;
    }

    public static DeleteFilterCommand FromDiff(ResourceDiff<GmailFilter> diff, string? commandId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string id = FilterOperationTranslator.ResolveDeleteId(diff) ?? throw new InvalidOperationException("Cannot delete filter without a valid ID.");
        return new DeleteFilterCommand(id, diff.Current, commandId);
    }
}
