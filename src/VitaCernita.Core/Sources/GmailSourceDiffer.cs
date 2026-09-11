using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.AutoReply.Diff;
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

    /// <summary>
    /// Compares auto-reply (vacation responder) configurations across any two abstract IGmailSource instances.
    /// </summary>
    public static async Task<AutoReplyDiff> DiffAutoReplyAsync(
        IGmailSource currentSource,
        IGmailSource desiredSource,
        AutoReplyDiffOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (currentSource == null) throw new ArgumentNullException(nameof(currentSource));
        if (desiredSource == null) throw new ArgumentNullException(nameof(desiredSource));

        options ??= new AutoReplyDiffOptions();

        var current = await currentSource.GetAutoReplyAsync(cancellationToken);
        var desired = await desiredSource.GetAutoReplyAsync(cancellationToken);

        return AutoReplyDiffer.Diff(current, desired, options);
    }

    /// <summary>
    /// Compares auto-reply configurations using LINQ query expressions across any two abstract IGmailSource instances.
    /// </summary>
    public static async Task<AutoReplyDiff> DiffAutoReplyAsync(
        IGmailSource currentSource,
        IGmailSource desiredSource,
        Func<IQueryable<AutoReply.AutoReply>, IQueryable<AutoReply.AutoReply>>? currentFilter,
        Func<IQueryable<AutoReply.AutoReply>, IQueryable<AutoReply.AutoReply>>? desiredFilter,
        AutoReplyDiffOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (currentSource == null) throw new ArgumentNullException(nameof(currentSource));
        if (desiredSource == null) throw new ArgumentNullException(nameof(desiredSource));

        options ??= new AutoReplyDiffOptions();

        var currentQuery = await currentSource.GetAutoRepliesAsync(cancellationToken);
        var desiredQuery = await desiredSource.GetAutoRepliesAsync(cancellationToken);

        if (currentFilter != null) currentQuery = currentFilter(currentQuery);
        if (desiredFilter != null) desiredQuery = desiredFilter(desiredQuery);

        var current = currentQuery.FirstOrDefault();
        var desired = desiredQuery.FirstOrDefault();

        return AutoReplyDiffer.Diff(current, desired, options);
    }
}
