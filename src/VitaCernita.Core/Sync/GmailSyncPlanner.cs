using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.Api;
using VitaCernita.Core.AutoReply.Diff;
using VitaCernita.Core.Filters.Diff;
using VitaCernita.Core.Labels.Diff;
using VitaCernita.Core.Sources;
using VitaCernita.Core.Sync.Commands;

namespace VitaCernita.Core.Sync;

/// <summary>
/// Synchronizes Gmail mailbox configurations by translating diffs between two sources
/// into an ordered sequence of executable commands to make the target match the desired specification.
/// </summary>
public static class GmailSyncPlanner
{
    /// <summary>
    /// Builds an ordered synchronization plan from existing diff results.
    /// Commands are structured to make the target (right) side match the reference (left) side.
    /// </summary>
    public static SyncPlan BuildPlan(
        LabelSetDiff? labelDiff,
        FilterSetDiff? filterDiff,
        AutoReplyDiff? autoReplyDiff,
        SyncPlanOptions? options = null)
    {
        options ??= new SyncPlanOptions();
        var commands = new List<ISyncCommand>();

        // 1. Labels to Create (labels must exist before any filters reference them)
        if (options.IncludeLabels && labelDiff != null)
        {
            foreach (var item in labelDiff.Creations)
            {
                if (item.DesiredLabel != null)
                {
                    commands.Add(new CreateLabelCommand(item.DesiredLabel));
                }
            }
        }

        // 2. Labels to Update (update names, visibility, colors of existing labels)
        if (options.IncludeLabels && labelDiff != null)
        {
            foreach (var item in labelDiff.Modifications)
            {
                commands.Add(UpdateLabelCommand.FromDiff(item));
            }
        }

        // 3. Filters to Update (matching filters by ID that have altered query or action)
        if (options.IncludeFilters && filterDiff != null)
        {
            foreach (var item in filterDiff.Modifications)
            {
                commands.Add(UpdateFilterCommand.FromDiff(item));
            }
        }

        // 4. Filters to Create (new filters)
        if (options.IncludeFilters && filterDiff != null)
        {
            foreach (var item in filterDiff.Creations)
            {
                if (item.DesiredFilter != null)
                {
                    commands.Add(CreateFilterCommand.FromDiff(item));
                }
            }
        }

        // 5. Filters to Delete (obsolete filters removed first before deleting labels they might reference)
        if (options.IncludeFilters && options.AllowDeletions && filterDiff != null)
        {
            foreach (var item in filterDiff.Deletions)
            {
                commands.Add(DeleteFilterCommand.FromDiff(item));
            }
        }

        // 6. Labels to Delete (obsolete labels removed last)
        if (options.IncludeLabels && options.AllowDeletions && labelDiff != null)
        {
            foreach (var item in labelDiff.Deletions)
            {
                commands.Add(DeleteLabelCommand.FromDiff(item));
            }
        }

        // 7. Auto-Reply Updates
        if (options.IncludeAutoReply && autoReplyDiff != null && autoReplyDiff.HasChanges)
        {
            if (autoReplyDiff.DiffType == AutoReplyDiffType.Disabled && !options.AllowDeletions)
            {
                // Disabling auto-reply is skipped if deletions/removals are not allowed
            }
            else
            {
                commands.Add(UpdateAutoReplyCommand.FromDiff(autoReplyDiff));
            }
        }

        return new SyncPlan(commands, options.Direction, labelDiff, filterDiff, autoReplyDiff);
    }

    /// <summary>
    /// Compares two abstract IGmailSource instances and builds a synchronization plan
    /// to make the target source match the desired source.
    /// By default (Direction = MakeRightMatchLeft), the right source is updated to match the left source.
    /// </summary>
    public static async Task<SyncPlan> BuildPlanAsync(
        IGmailSource left,
        IGmailSource right,
        SyncPlanOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (left == null) throw new ArgumentNullException(nameof(left));
        if (right == null) throw new ArgumentNullException(nameof(right));

        options ??= new SyncPlanOptions();

        IGmailSource desiredSource;
        IGmailSource currentSource;

        if (options.Direction == SyncDirection.MakeRightMatchLeft)
        {
            desiredSource = left;
            currentSource = right;
        }
        else
        {
            desiredSource = right;
            currentSource = left;
        }

        LabelSetDiff? labelDiff = null;
        if (options.IncludeLabels)
        {
            labelDiff = await GmailSourceDiffer.DiffLabelsAsync(currentSource, desiredSource, options.LabelOptions, cancellationToken);
        }

        FilterSetDiff? filterDiff = null;
        if (options.IncludeFilters)
        {
            filterDiff = await GmailSourceDiffer.DiffFiltersAsync(currentSource, desiredSource, options.FilterOptions, cancellationToken);
        }

        AutoReplyDiff? autoReplyDiff = null;
        if (options.IncludeAutoReply)
        {
            autoReplyDiff = await GmailSourceDiffer.DiffAutoReplyAsync(currentSource, desiredSource, options.AutoReplyOptions, cancellationToken);
        }

        return BuildPlan(labelDiff, filterDiff, autoReplyDiff, options);
    }

    /// <summary>
    /// Builds a synchronization plan to make a target Gmail account match a Lua script configuration.
    /// </summary>
    public static Task<SyncPlan> BuildPlanFromScriptAsync(
        IGmailApiClient client,
        string luaScript,
        SyncPlanOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        var currentSource = new ApiGmailSource(client, userId);
        var desiredSource = LuaGmailSource.FromScript(luaScript);
        return BuildPlanAsync(desiredSource, currentSource, options, cancellationToken);
    }

    /// <summary>
    /// Builds a synchronization plan to make a target Gmail account match a Lua configuration file.
    /// </summary>
    public static Task<SyncPlan> BuildPlanFromFileAsync(
        IGmailApiClient client,
        string filePath,
        SyncPlanOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        var currentSource = new ApiGmailSource(client, userId);
        var desiredSource = new LuaGmailSource(filePath);
        return BuildPlanAsync(desiredSource, currentSource, options, cancellationToken);
    }
}
