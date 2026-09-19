using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Diffing.Labels;

/// <summary>
/// Represents the computed differences between two collections of Gmail labels.
/// </summary>
public sealed class LabelSetDiff : ResourceSetDiff<GmailLabel>
{
    public new IReadOnlyList<LabelDiff> Differences { get; }
    public new IReadOnlyList<LabelDiff> Creations => Differences.Where(d => d.DiffType == DiffKind.Added).ToList();
    public new IReadOnlyList<LabelDiff> Deletions => Differences.Where(d => d.DiffType == DiffKind.Removed).ToList();
    public new IReadOnlyList<LabelDiff> Modifications => Differences.Where(d => d.DiffType == DiffKind.Modified).ToList();
    public new IReadOnlyList<LabelDiff> Unchanged => Differences.Where(d => d.DiffType == DiffKind.Unchanged).ToList();

    public LabelSetDiff(IEnumerable<LabelDiff> differences)
        : base((differences ?? Array.Empty<LabelDiff>()).Cast<ResourceDiff<GmailLabel>>())
    {
        Differences = (differences ?? Array.Empty<LabelDiff>()).ToList();
    }

    public string ToSummaryString()
    {
        return $"Summary: {TotalCreations} to create, {TotalModifications} to update, {TotalDeletions} to delete, {TotalUnchanged} unchanged.";
    }

    /// <summary>
    /// Generates a comprehensive, human-readable dry-run report.
    /// </summary>
    public string ToDryRunReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("======================================================================");
        sb.AppendLine("VitaCernita Label Diff Report (Dry Run)");
        sb.AppendLine("======================================================================");
        sb.AppendLine(ToSummaryString());
        sb.AppendLine();

        if (TotalCreations > 0)
        {
            sb.AppendLine($"[+] Create ({TotalCreations}):");
            foreach (var item in Creations)
            {
                var label = item.DesiredLabel;
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

        if (TotalModifications > 0)
        {
            sb.AppendLine($"[~] Update ({TotalModifications}):");
            foreach (var item in Modifications)
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

        if (TotalDeletions > 0)
        {
            sb.AppendLine($"[-] Delete ({TotalDeletions}):");
            foreach (var item in Deletions)
            {
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  - '{item.Name}'{idStr}");
            }
            sb.AppendLine();
        }

        if (TotalUnchanged > 0)
        {
            sb.AppendLine($"[=] Unchanged ({TotalUnchanged}):");
            foreach (var item in Unchanged)
            {
                string idStr = item.Id != null ? $" (ID: {item.Id})" : "";
                sb.AppendLine($"  = '{item.Name}'{idStr}");
            }
            sb.AppendLine();
        }

        sb.Append("======================================================================");
        return sb.ToString();
    }

    public override string ToString() => ToSummaryString();
}
