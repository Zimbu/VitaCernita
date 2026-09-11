using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VitaCernita.Core.AutoReply.Diff;
using VitaCernita.Core.Filters.Diff;
using VitaCernita.Core.Labels.Diff;

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
    public LabelSetDiff? LabelDiff { get; }
    public FilterSetDiff? FilterDiff { get; }
    public AutoReplyDiff? AutoReplyDiff { get; }

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
        LabelSetDiff? labelDiff = null,
        FilterSetDiff? filterDiff = null,
        AutoReplyDiff? autoReplyDiff = null)
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

    /// <summary>
    /// Generates a comprehensive, human-readable dry-run report of all planned execution commands.
    /// </summary>
    public string ToDryRunReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("======================================================================");
        sb.AppendLine("VitaCernita Synchronization Plan (Dry Run)");
        sb.AppendLine("======================================================================");
        sb.AppendLine(ToSummaryString());

        int labelCount = LabelCommands.Count;
        int filterCount = FilterCommands.Count;
        int autoReplyCount = AutoReplyCommands.Count;

        sb.AppendLine($"  - Labels    : {LabelCommands.Count(c => c.ActionType == SyncActionType.Create)} create, {LabelCommands.Count(c => c.ActionType == SyncActionType.Update)} update, {LabelCommands.Count(c => c.ActionType == SyncActionType.Delete)} delete");
        sb.AppendLine($"  - Filters   : {FilterCommands.Count(c => c.ActionType == SyncActionType.Create)} create, {FilterCommands.Count(c => c.ActionType == SyncActionType.Update)} update, {FilterCommands.Count(c => c.ActionType == SyncActionType.Delete)} delete");
        sb.AppendLine($"  - Auto-Reply: {AutoReplyCommands.Count} action(s)");
        sb.AppendLine();

        if (IsEmpty)
        {
            sb.AppendLine("  No changes required. Configurations are completely in sync.");
            sb.AppendLine("======================================================================");
            return sb.ToString();
        }

        sb.AppendLine("Planned Execution Steps (Dependency-Safe Order):");
        for (int i = 0; i < Commands.Count; i++)
        {
            var cmd = Commands[i];
            sb.AppendLine($"  {i + 1,2}. {cmd.ToDryRunString()}");
            if (cmd.DetailedDescription != cmd.Description)
            {
                var lines = cmd.DetailedDescription.Split('\n');
                for (int l = 1; l < lines.Length; l++)
                {
                    string line = lines[l].TrimEnd();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        sb.AppendLine($"         {line}");
                    }
                }
            }
        }

        sb.AppendLine("======================================================================");
        return sb.ToString();
    }

    public override string ToString() => ToSummaryString();
}
