using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VitaCernita.Core.Filters;

namespace VitaCernita.Core.Diffing.Filters;

/// <summary>
/// Represents the computed differences between two collections of Gmail filters.
/// </summary>
public sealed class FilterSetDiff : ResourceSetDiff<GmailFilter>
{
    public FilterSetDiff(IEnumerable<ResourceDiff<GmailFilter>>? differences)
        : base(differences ?? Array.Empty<ResourceDiff<GmailFilter>>())
    {
    }

    public string ToSummaryString()
    {
        return $"Summary: {TotalCreations} to create, {TotalModifications} to update, {TotalDeletions} to delete, {TotalUnchanged} unchanged.";
    }

    /// <summary>
    /// Generates a comprehensive, human-readable dry-run report of filter differences.
    /// </summary>
    public string ToDryRunReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("======================================================================");
        sb.AppendLine("VitaCernita Filter Diff Report (Dry Run)");
        sb.AppendLine("======================================================================");
        sb.AppendLine(ToSummaryString());
        sb.AppendLine();

        if (TotalCreations > 0)
        {
            sb.AppendLine($"[+] Create ({TotalCreations}):");
            foreach (var item in Creations)
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

        if (TotalModifications > 0)
        {
            sb.AppendLine($"[~] Update ({TotalModifications}):");
            foreach (var item in Modifications)
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

        if (TotalDeletions > 0)
        {
            sb.AppendLine($"[-] Delete ({TotalDeletions}):");
            foreach (var item in Deletions)
            {
                string nameStr = !string.IsNullOrWhiteSpace(item.Identifier) ? $" '{item.Identifier}'" : "";
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                string queryStr = item.Current?.ToGmailQuery() ?? "<none>";
                sb.AppendLine($"  - Filter{nameStr}{idStr}: query '{queryStr}'");
            }
            sb.AppendLine();
        }

        if (TotalUnchanged > 0)
        {
            sb.AppendLine($"[=] Unchanged ({TotalUnchanged}):");
            foreach (var item in Unchanged)
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

    public override string ToString() => ToSummaryString();
}
