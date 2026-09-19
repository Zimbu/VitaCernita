using System.Text;
using VitaCernita.Core.Diffing;
using AutoReplyModel = VitaCernita.Core.AutoReply.AutoReply;

namespace VitaCernita.Core.Diffing.AutoReply;

/// <summary>
/// Extension methods for reporting and formatting AutoReply resource diffs.
/// </summary>
public static class AutoReplyDiffExtensions
{
    /// <summary>
    /// Generates a human-readable report suitable for CLI dry-run and diff summaries.
    /// </summary>
    public static string ToDryRunReport(this ResourceDiff<AutoReplyModel> diff)
    {
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
}
