using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Sync;
using AutoReplyModel = VitaCernita.Core.AutoReply.AutoReply;

namespace VitaCernita.Core.Reporting;

/// <summary>
/// Encapsulates formatting and generation of comprehensive, human-readable dry-run reports
/// for label diffs, filter diffs, auto-reply diffs, and synchronization execution plans.
/// </summary>
public static class DryRunReportGenerator
{
    /// <summary>
    /// Generates a comprehensive, unified dry-run report across all four synchronization components:
    /// labels, filters, auto-reply, and the planned sync commands.
    /// </summary>
    public static string CreateReport(
        ResourceSetDiff<GmailLabel>? labelDiff,
        ResourceSetDiff<GmailFilter>? filterDiff,
        ResourceDiff<AutoReplyModel>? autoReplyDiff,
        SyncPlan? syncPlan)
    {
        var sb = new StringBuilder();

        if (labelDiff != null)
        {
            sb.AppendLine(FormatLabelReport(labelDiff));
        }

        if (filterDiff != null)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine(FormatFilterReport(filterDiff));
        }

        if (autoReplyDiff != null && (autoReplyDiff.HasChanges || autoReplyDiff.AccountError != null))
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine(FormatAutoReplyReport(autoReplyDiff));
        }

        if (syncPlan != null)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append(FormatSyncPlanReport(syncPlan));
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Generates a dry-run report for a label collection diff.
    /// </summary>
    public static string CreateLabelReport(ResourceSetDiff<GmailLabel> diff) =>
        FormatLabelReport(diff);

    /// <summary>
    /// Generates a dry-run report for a filter collection diff.
    /// </summary>
    public static string CreateFilterReport(ResourceSetDiff<GmailFilter> diff) =>
        FormatFilterReport(diff);

    /// <summary>
    /// Generates a dry-run report for an auto-reply diff.
    /// </summary>
    public static string CreateAutoReplyReport(ResourceDiff<AutoReplyModel> diff) =>
        FormatAutoReplyReport(diff);

    /// <summary>
    /// Generates a dry-run report for a synchronization plan.
    /// </summary>
    public static string CreateSyncPlanReport(SyncPlan plan) =>
        FormatSyncPlanReport(plan);

    private static string LabelSetToSummaryString(ResourceSetDiff<GmailLabel> diff) =>
        $"Summary: {diff.TotalCreations} to create, {diff.TotalModifications} to update, {diff.TotalDeletions} to delete, {diff.TotalUnchanged} unchanged.";

    private static string FormatLabelReport(ResourceSetDiff<GmailLabel> diff)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));

        var sb = new StringBuilder();
        sb.AppendLine("======================================================================");
        sb.AppendLine("VitaCernita Label Diff Report (Dry Run)");
        sb.AppendLine("======================================================================");
        sb.AppendLine(LabelSetToSummaryString(diff));
        sb.AppendLine();

        if (diff.TotalCreations > 0)
        {
            sb.AppendLine($"[+] Create ({diff.TotalCreations}):");
            foreach (var item in diff.Creations)
            {
                var label = item.Desired;
                var details = new List<string>();
                if (!string.IsNullOrWhiteSpace(label?.MessageListVisibility))
                    details.Add($"MessageList: {label.MessageListVisibility}");
                if (!string.IsNullOrWhiteSpace(label?.LabelListVisibility))
                    details.Add($"LabelList: {label.LabelListVisibility}");
                if (label?.Color != null)
                    details.Add($"Color: [{label.Color.TextColor} / {label.Color.BackgroundColor}]");

                string detailStr = details.Count > 0 ? $" ({string.Join(", ", details)})" : "";
                sb.AppendLine($"  + '{item.Name}'{detailStr}");
            }
            sb.AppendLine();
        }

        if (diff.TotalModifications > 0)
        {
            sb.AppendLine($"[~] Update ({diff.TotalModifications}):");
            foreach (var item in diff.Modifications)
            {
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  ~ '{item.Name}'{idStr}:");
                foreach (var fieldDiff in item.FieldDifferences)
                {
                    sb.AppendLine($"      * {fieldDiff}");
                }
            }
            sb.AppendLine();
        }

        if (diff.TotalDeletions > 0)
        {
            sb.AppendLine($"[-] Delete ({diff.TotalDeletions}):");
            foreach (var item in diff.Deletions)
            {
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  - '{item.Name}'{idStr}");
            }
            sb.AppendLine();
        }

        if (diff.TotalUnchanged > 0)
        {
            sb.AppendLine($"[=] Unchanged ({diff.TotalUnchanged}):");
            foreach (var item in diff.Unchanged)
            {
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  = '{item.Name}'{idStr}");
            }
            sb.AppendLine();
        }

        sb.Append("======================================================================");
        return sb.ToString();
    }

    private static string FilterSetToSummaryString(ResourceSetDiff<GmailFilter> diff) =>
        $"Summary: {diff.TotalCreations} to create, {diff.TotalModifications} to update, {diff.TotalDeletions} to delete, {diff.TotalUnchanged} unchanged.";

    private static string FormatFilterReport(ResourceSetDiff<GmailFilter> diff)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));

        var sb = new StringBuilder();
        sb.AppendLine("======================================================================");
        sb.AppendLine("VitaCernita Filter Diff Report (Dry Run)");
        sb.AppendLine("======================================================================");
        sb.AppendLine(FilterSetToSummaryString(diff));
        sb.AppendLine();

        if (diff.TotalCreations > 0)
        {
            sb.AppendLine($"[+] Create ({diff.TotalCreations}):");
            foreach (var item in diff.Creations)
            {
                string nameStr = !string.IsNullOrWhiteSpace(item.Identifier) ? $" '{item.Identifier}'" : "";
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                string queryStr = item.Desired?.ToGmailQuery() ?? "<none>";
                string actionStr = item.Desired?.Action?.ToString() ?? "<none>";
                sb.AppendLine($"  + Filter{nameStr}{idStr}:");
                sb.AppendLine($"      Query : {queryStr}");
                sb.AppendLine($"      Action: {actionStr}");
            }
            sb.AppendLine();
        }

        if (diff.TotalModifications > 0)
        {
            sb.AppendLine($"[~] Update ({diff.TotalModifications}):");
            foreach (var item in diff.Modifications)
            {
                string nameStr = !string.IsNullOrWhiteSpace(item.Identifier) ? $" '{item.Identifier}'" : "";
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  ~ Filter{nameStr}{idStr}:");
                foreach (var fieldDiff in item.FieldDifferences)
                {
                    sb.AppendLine($"      * {fieldDiff}");
                }
            }
            sb.AppendLine();
        }

        if (diff.TotalDeletions > 0)
        {
            sb.AppendLine($"[-] Delete ({diff.TotalDeletions}):");
            foreach (var item in diff.Deletions)
            {
                string nameStr = !string.IsNullOrWhiteSpace(item.Identifier) ? $" '{item.Identifier}'" : "";
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                string queryStr = item.Current?.ToGmailQuery() ?? "<none>";
                sb.AppendLine($"  - Filter{nameStr}{idStr}: query '{queryStr}'");
            }
            sb.AppendLine();
        }

        if (diff.TotalUnchanged > 0)
        {
            sb.AppendLine($"[=] Unchanged ({diff.TotalUnchanged}):");
            foreach (var item in diff.Unchanged)
            {
                string nameStr = !string.IsNullOrWhiteSpace(item.Identifier) ? $" '{item.Identifier}'" : "";
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  = Filter{nameStr}{idStr}");
            }
            sb.AppendLine();
        }

        sb.Append("======================================================================");
        return sb.ToString();
    }

    private static string FormatAutoReplyReport(ResourceDiff<AutoReplyModel> diff)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));

        var sb = new StringBuilder();
        sb.AppendLine("Auto-Reply (Vacation Responder) Diff:");

        if (diff.Error != null)
        {
            sb.AppendLine($"  [ERROR] {diff.Error}");
        }

        switch (diff.DiffType)
        {
            case DiffKind.Unchanged:
                sb.AppendLine("  Status: Unchanged");
                if (diff.Desired != null && diff.Desired.EnableAutoReply)
                {
                    sb.AppendLine($"  Active Subject: '{diff.Desired.ResponseSubject}'");
                }
                break;

            case DiffKind.Added:
                sb.AppendLine("  Status: + Enable Auto-Reply (Create / Turn On)");
                if (diff.Desired != null)
                {
                    sb.AppendLine($"    Subject     : '{diff.Desired.ResponseSubject}'");
                    if (!string.IsNullOrWhiteSpace(diff.Desired.ResponseBodyPlainText))
                        sb.AppendLine($"    Body (Plain): '{diff.Desired.ResponseBodyPlainText.Replace("\n", " ")}'");
                    if (!string.IsNullOrWhiteSpace(diff.Desired.ResponseBodyHtml))
                        sb.AppendLine($"    Body (HTML) : '{diff.Desired.ResponseBodyHtml.Replace("\n", " ")}'");
                    sb.AppendLine($"    ContactsOnly: {diff.Desired.RestrictToContacts}");
                    sb.AppendLine($"    DomainOnly  : {diff.Desired.RestrictToDomain}");
                    if (diff.Desired.StartTime.HasValue)
                        sb.AppendLine($"    Start Time  : {diff.Desired.StartDateTime:yyyy-MM-dd HH:mm:ss} UTC");
                    if (diff.Desired.EndTime.HasValue)
                        sb.AppendLine($"    End Time    : {diff.Desired.EndDateTime:yyyy-MM-dd HH:mm:ss} UTC");
                }
                break;

            case DiffKind.Disabled:
                sb.AppendLine("  Status: - Turn Off Auto-Reply");
                if (diff.Current != null && !string.IsNullOrWhiteSpace(diff.Current.ResponseSubject))
                {
                    sb.AppendLine($"    Previous Subject: '{diff.Current.ResponseSubject}'");
                }
                break;

            case DiffKind.Modified:
                sb.AppendLine($"  Status: ~ Update Auto-Reply ({diff.FieldDifferences.Count} change(s)):");
                foreach (var field in diff.FieldDifferences)
                {
                    sb.AppendLine($"    ~ {field}");
                }
                break;
        }

        return sb.ToString().TrimEnd();
    }

    private static string SyncPlanToSummaryString(SyncPlan plan) =>
        plan.ToSummaryString();

    private static string FormatSyncPlanReport(SyncPlan plan)
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));

        var sb = new StringBuilder();
        sb.AppendLine("======================================================================");
        sb.AppendLine("VitaCernita Synchronization Plan (Dry Run)");
        sb.AppendLine("======================================================================");
        sb.AppendLine(SyncPlanToSummaryString(plan));

        sb.AppendLine($"  - Labels    : {plan.LabelCommands.Count(c => c.ActionType == SyncActionType.Create)} create, {plan.LabelCommands.Count(c => c.ActionType == SyncActionType.Update)} update, {plan.LabelCommands.Count(c => c.ActionType == SyncActionType.Delete)} delete");
        sb.AppendLine($"  - Filters   : {plan.FilterCommands.Count(c => c.ActionType == SyncActionType.Create)} create, {plan.FilterCommands.Count(c => c.ActionType == SyncActionType.Update)} update, {plan.FilterCommands.Count(c => c.ActionType == SyncActionType.Delete)} delete");
        sb.AppendLine($"  - Auto-Reply: {plan.AutoReplyCommands.Count} action(s)");
        sb.AppendLine();

        if (plan.IsEmpty)
        {
            sb.AppendLine("  No changes required. Configurations are completely in sync.");
            sb.AppendLine("======================================================================");
            return sb.ToString();
        }

        sb.AppendLine("Planned Execution Steps (Dependency-Safe Order):");
        for (int i = 0; i < plan.Commands.Count; i++)
        {
            var cmd = plan.Commands[i];
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
}
