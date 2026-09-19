using System.Collections.Generic;
using VitaCernita.Core.Diffing.AutoReply;
using VitaCernita.Core.Diffing.Filters;
using VitaCernita.Core.Diffing.Labels;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Sync;

/// <summary>
/// Options configuring synchronization plan generation.
/// </summary>
public sealed class SyncPlanOptions
{
    /// <summary>
    /// The direction of synchronization. Default is <see cref="SyncDirection.MakeRightMatchLeft"/>
    /// (the right target source is mutated to match the left reference source).
    /// </summary>
    public SyncDirection Direction { get; set; } = SyncDirection.MakeRightMatchLeft;

    /// <summary>
    /// Whether to generate delete commands for resources present in the target but absent in the reference source.
    /// Default is true. Set to false for additive-only synchronization.
    /// </summary>
    public bool AllowDeletions { get; set; } = true;

    /// <summary>
    /// Whether to include Gmail labels in the synchronization plan. Default is true.
    /// </summary>
    public bool IncludeLabels { get; set; } = true;

    /// <summary>
    /// Whether to include search filters in the synchronization plan. Default is true.
    /// </summary>
    public bool IncludeFilters { get; set; } = true;

    /// <summary>
    /// Whether to include auto-reply (vacation responder) in the synchronization plan. Default is true.
    /// </summary>
    public bool IncludeAutoReply { get; set; } = true;

    /// <summary>
    /// Optional custom options for diffing labels.
    /// </summary>
    public LabelDiffOptions? LabelOptions { get; set; }

    /// <summary>
    /// Optional custom options for diffing filters.
    /// </summary>
    public FilterDiffOptions? FilterOptions { get; set; }

    /// <summary>
    /// Optional custom options for diffing auto-reply.
    /// </summary>
    public AutoReplyDiffOptions? AutoReplyOptions { get; set; }

    /// <summary>
    /// Known Gmail labels used to resolve internal label IDs to human-readable label names
    /// when generating sync commands and filter API payloads.
    /// </summary>
    public IEnumerable<GmailLabel>? KnownLabels { get; set; }
}
