using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Labels.Diff;

namespace VitaCernita.Core.Sources;

/// <summary>
/// Compares Gmail mailbox configurations across any two abstract IGmailSource instances.
/// Completely agnostic of underlying data source (Lua, Live API, In-Memory Fake, Snapshot).
/// Supports LINQ-based subset filtering before diffing.
/// </summary>
public static class GmailSourceDiffer
{
    /// <summary>
    /// Compares labels from a current source against a desired source.
    /// </summary>
    public static async Task<LabelSetDiff> DiffLabelsAsync(
        IGmailSource currentSource,
        IGmailSource desiredSource,
        LabelDiffOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (currentSource == null) throw new ArgumentNullException(nameof(currentSource));
        if (desiredSource == null) throw new ArgumentNullException(nameof(desiredSource));

        var current = await currentSource.GetLabelsAsync(cancellationToken);
        var desired = await desiredSource.GetLabelsAsync(cancellationToken);

        return GmailLabelDiffer.DiffSets(current, desired, options);
    }

    /// <summary>
    /// Compares subsets of labels using LINQ filter expressions on both sources before diffing.
    /// </summary>
    public static async Task<LabelSetDiff> DiffLabelsAsync(
        IGmailSource currentSource,
        IGmailSource desiredSource,
        Func<IQueryable<GmailLabel>, IQueryable<GmailLabel>>? currentFilter,
        Func<IQueryable<GmailLabel>, IQueryable<GmailLabel>>? desiredFilter,
        LabelDiffOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (currentSource == null) throw new ArgumentNullException(nameof(currentSource));
        if (desiredSource == null) throw new ArgumentNullException(nameof(desiredSource));

        var current = await currentSource.GetLabelsAsync(cancellationToken);
        var desired = await desiredSource.GetLabelsAsync(cancellationToken);

        if (currentFilter != null) current = currentFilter(current);
        if (desiredFilter != null) desired = desiredFilter(desired);

        return GmailLabelDiffer.DiffSets(current, desired, options);
    }
}
