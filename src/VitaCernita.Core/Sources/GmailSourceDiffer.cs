using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Diffing.AutoReply;
using VitaCernita.Core.Diffing.Filters;
using VitaCernita.Core.Diffing.Labels;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;

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
    /// Compares search filters from a current source against a desired source.
    /// </summary>
    public static Task<FilterSetDiff> DiffFiltersAsync(
        IGmailSource currentSource,
        IGmailSource desiredSource,
        FilterDiffOptions? options = null,
        CancellationToken cancellationToken = default) =>
        DiffFiltersAsync(currentSource, desiredSource, currentFilter: null, desiredFilter: null, options, cancellationToken);

    /// <summary>
    /// Compares subsets of filters using LINQ filter expressions on both sources before diffing.
    /// </summary>
    public static async Task<FilterSetDiff> DiffFiltersAsync(
        IGmailSource currentSource,
        IGmailSource desiredSource,
        Func<IQueryable<GmailFilter>, IQueryable<GmailFilter>>? currentFilter,
        Func<IQueryable<GmailFilter>, IQueryable<GmailFilter>>? desiredFilter,
        FilterDiffOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (currentSource == null) throw new ArgumentNullException(nameof(currentSource));
        if (desiredSource == null) throw new ArgumentNullException(nameof(desiredSource));

        var current = await currentSource.GetFiltersAsync(cancellationToken);
        var desired = await desiredSource.GetFiltersAsync(cancellationToken);

        if (currentFilter != null) current = currentFilter(current);
        if (desiredFilter != null) desired = desiredFilter(desired);

        var effectiveOptions = options ?? new FilterDiffOptions();
        if (effectiveOptions.KnownLabels == null)
        {
            var currentLabels = await currentSource.GetLabelsAsync(cancellationToken);
            var desiredLabels = await desiredSource.GetLabelsAsync(cancellationToken);
            var combined = currentLabels.Concat(desiredLabels).ToList();
            if (combined.Count > 0)
            {
                effectiveOptions = new FilterDiffOptions
                {
                    MatchBy = effectiveOptions.MatchBy,
                    IncludeUnchanged = effectiveOptions.IncludeUnchanged,
                    CompareName = effectiveOptions.CompareName,
                    FieldsToCompare = effectiveOptions.FieldsToCompare,
                    FieldsToIgnore = effectiveOptions.FieldsToIgnore,
                    KnownLabels = combined
                };
            }
        }

        return GmailFilterDiffer.DiffSets(current, desired, effectiveOptions);
    }

    /// <summary>
    /// Compares auto-reply (vacation responder) configurations across any two abstract IGmailSource instances.
    /// </summary>
    public static async Task<ResourceDiff<AutoReply.AutoReply>> DiffAutoReplyAsync(
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
    public static async Task<ResourceDiff<AutoReply.AutoReply>> DiffAutoReplyAsync(
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
