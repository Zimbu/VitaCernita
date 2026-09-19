using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Diffing.AutoReply;
using VitaCernita.Core.Diffing.Filters;
using VitaCernita.Core.Diffing.Labels;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Sync;

/// <summary>
/// Represents an ordered, dependency-safe plan of synchronization commands
/// designed to make a target Gmail account match a desired configuration.
/// Suitable for dry-run inspection and reporting.
/// </summary>
public sealed class SyncPlan
{
    public IReadOnlyList<ISyncCommand> Commands { get; }
    public SyncDirection Direction { get; }
    public ResourceSetDiff<GmailLabel>? LabelDiff { get; }
    public ResourceSetDiff<GmailFilter>? FilterDiff { get; }
    public ResourceDiff<VitaCernita.Core.AutoReply.AutoReply>? AutoReplyDiff { get; }

    public bool IsEmpty => Commands.Count == 0;
    public int TotalCommands => Commands.Count;

    public IReadOnlyList<ISyncCommand> LabelCommands => Commands.Where(c => c.ResourceType == SyncResourceType.Label).ToList();
    public IReadOnlyList<ISyncCommand> FilterCommands => Commands.Where(c => c.ResourceType == SyncResourceType.Filter).ToList();
    public IReadOnlyList<ISyncCommand> AutoReplyCommands => Commands.Where(c => c.ResourceType == SyncResourceType.AutoReply).ToList();

    public IReadOnlyList<ISyncCommand> Creations => Commands.Where(c => c.ActionType == SyncActionType.Create).ToList();
    public IReadOnlyList<ISyncCommand> Updates => Commands.Where(c => c.ActionType == SyncActionType.Update).ToList();
    public IReadOnlyList<ISyncCommand> Deletions => Commands.Where(c => c.ActionType == SyncActionType.Delete).ToList();

    public int TotalCreations => Creations.Count;
    public int TotalUpdates => Updates.Count;
    public int TotalDeletions => Deletions.Count;

    public SyncPlan(
        IEnumerable<ISyncCommand> commands,
        SyncDirection direction = SyncDirection.MakeRightMatchLeft,
        ResourceSetDiff<GmailLabel>? labelDiff = null,
        ResourceSetDiff<GmailFilter>? filterDiff = null,
        ResourceDiff<VitaCernita.Core.AutoReply.AutoReply>? autoReplyDiff = null)
    {
        Commands = (commands ?? Array.Empty<ISyncCommand>()).ToList();
        Direction = direction;
        LabelDiff = labelDiff;
        FilterDiff = filterDiff;
        AutoReplyDiff = autoReplyDiff;
    }

    public string ToSummaryString()
    {
        string dirStr = Direction == SyncDirection.MakeRightMatchLeft
            ? "Make Right match Left"
            : "Make Left match Right";

        return $"Sync Plan ({dirStr}): {TotalCommands} commands ({TotalCreations} create, {TotalUpdates} update, {TotalDeletions} delete).";
    }

    public override string ToString() => ToSummaryString();
}
