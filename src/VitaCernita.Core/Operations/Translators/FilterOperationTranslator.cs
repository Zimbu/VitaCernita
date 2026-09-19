using System;
using System.Collections.Generic;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Operations.Filters;

namespace VitaCernita.Core.Operations.Translators;

/// <summary>
/// Translates a pure model ResourceDiff&lt;GmailFilter&gt; into executable Gmail REST API operations
/// and constructs Gmail-specific users.settings.filters API payloads.
/// </summary>
public static class FilterOperationTranslator
{
    /// <summary>
    /// Generates a JSON-ready dictionary payload for a Gmail API users.settings.filters.create call.
    /// </summary>
    public static Dictionary<string, object>? BuildCreatePayload(
        ResourceDiff<GmailFilter> diff,
        bool explicitAnd = false,
        IEnumerable<GmailLabel>? knownLabels = null)
    {
        return diff?.Desired?.ToDictionary(knownLabels, explicitAnd);
    }

    /// <summary>
    /// Returns the target filter ID to delete for a Gmail API users.settings.filters.delete call.
    /// </summary>
    public static string? ResolveDeleteId(ResourceDiff<GmailFilter> diff)
    {
        return diff?.Current?.Id ?? diff?.Id;
    }

    /// <summary>
    /// Translates a filter creation diff into a CreateFilterOperation.
    /// </summary>
    public static CreateFilterOperation ToCreateOperation(
        ResourceDiff<GmailFilter> diff,
        string? operationId = null,
        IEnumerable<GmailLabel>? knownLabels = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        if (diff.Desired == null) throw new InvalidOperationException("Cannot create filter without a desired filter specification.");
        return new CreateFilterOperation(diff.Desired, operationId, knownLabels);
    }

    /// <summary>
    /// Translates a filter modification diff into an UpdateFilterOperation.
    /// </summary>
    public static UpdateFilterOperation ToUpdateOperation(
        ResourceDiff<GmailFilter> diff,
        string? operationId = null,
        IEnumerable<GmailLabel>? knownLabels = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string id = diff.Id ?? diff.Current?.Id ?? throw new InvalidOperationException("Cannot update filter without a valid ID.");
        if (diff.Current == null) throw new InvalidOperationException("Cannot update filter without current filter state.");
        if (diff.Desired == null) throw new InvalidOperationException("Cannot update filter without desired filter state.");

        return new UpdateFilterOperation(
            id,
            diff.Current,
            diff.Desired,
            diff.FieldDifferences,
            operationId,
            knownLabels);
    }

    /// <summary>
    /// Translates a filter deletion diff into a DeleteFilterOperation.
    /// </summary>
    public static DeleteFilterOperation ToDeleteOperation(
        ResourceDiff<GmailFilter> diff,
        string? operationId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string id = ResolveDeleteId(diff) ?? throw new InvalidOperationException("Cannot delete filter without a valid ID.");
        return new DeleteFilterOperation(id, diff.Identifier, operationId);
    }

    /// <summary>
    /// Translates any single ResourceDiff into its corresponding IGmailOperation (or null if unchanged).
    /// </summary>
    public static IGmailOperation? Translate(
        ResourceDiff<GmailFilter> diff,
        string? operationId = null,
        IEnumerable<GmailLabel>? knownLabels = null)
    {
        return diff.DiffType switch
        {
            DiffKind.Added => ToCreateOperation(diff, operationId, knownLabels),
            DiffKind.Modified => ToUpdateOperation(diff, operationId, knownLabels),
            DiffKind.Removed => ToDeleteOperation(diff, operationId),
            _ => null
        };
    }
}
