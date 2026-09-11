using System;
using System.Collections.Generic;
using System.Text;

namespace VitaCernita.Core.AutoReply.Diff;

/// <summary>
/// Represents the computed difference between current and desired AutoReply (VacationSettings) configurations.
/// </summary>
public sealed class AutoReplyDiff
{
    public AutoReplyDiffType DiffType { get; }
    public AutoReply? CurrentAutoReply { get; }
    public AutoReply? DesiredAutoReply { get; }
    public IReadOnlyList<AutoReplyFieldDiff> FieldDifferences { get; }
    public string? AccountError { get; set; }

    public bool HasChanges => DiffType != AutoReplyDiffType.Unchanged;

    public AutoReplyDiff(
        AutoReplyDiffType diffType,
        AutoReply? currentAutoReply,
        AutoReply? desiredAutoReply,
        IReadOnlyList<AutoReplyFieldDiff>? fieldDifferences = null,
        string? accountError = null)
    {
        DiffType = diffType;
        CurrentAutoReply = currentAutoReply;
        DesiredAutoReply = desiredAutoReply;
        FieldDifferences = fieldDifferences ?? Array.Empty<AutoReplyFieldDiff>();
        AccountError = accountError;
    }

    /// <summary>
    /// Generates a payload suitable for PUT users.settings.updateVacation.
    /// </summary>
    public Dictionary<string, object>? GetUpdatePayload()
    {
        return DesiredAutoReply?.ToDictionary();
    }

    /// <summary>
    /// Generates a human-readable report suitable for CLI dry-run and diff summaries.
    /// </summary>
    public string ToDryRunReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Auto-Reply (Vacation Responder) Diff:");

        if (AccountError != null)
        {
            sb.AppendLine($"  [ERROR] {AccountError}");
        }

        switch (DiffType)
        {
            case AutoReplyDiffType.Unchanged:
                sb.AppendLine("  Status: Unchanged");
                if (DesiredAutoReply != null && DesiredAutoReply.EnableAutoReply)
                {
                    sb.AppendLine($"  Active Subject: '{DesiredAutoReply.ResponseSubject}'");
                }
                break;

            case AutoReplyDiffType.Added:
                sb.AppendLine("  Status: + Enable Auto-Reply (Create / Turn On)");
                if (DesiredAutoReply != null)
                {
                    sb.AppendLine($"    Subject     : '{DesiredAutoReply.ResponseSubject}'");
                    if (!string.IsNullOrWhiteSpace(DesiredAutoReply.ResponseBodyPlainText))
                        sb.AppendLine($"    Body (Plain): '{DesiredAutoReply.ResponseBodyPlainText.Replace("\n", " ")}'");
                    if (!string.IsNullOrWhiteSpace(DesiredAutoReply.ResponseBodyHtml))
                        sb.AppendLine($"    Body (HTML) : '{DesiredAutoReply.ResponseBodyHtml.Replace("\n", " ")}'");
                    sb.AppendLine($"    ContactsOnly: {DesiredAutoReply.RestrictToContacts}");
                    sb.AppendLine($"    DomainOnly  : {DesiredAutoReply.RestrictToDomain}");
                    if (DesiredAutoReply.StartTime.HasValue)
                        sb.AppendLine($"    Start Time  : {DesiredAutoReply.StartDateTime:yyyy-MM-dd HH:mm:ss} UTC");
                    if (DesiredAutoReply.EndTime.HasValue)
                        sb.AppendLine($"    End Time    : {DesiredAutoReply.EndDateTime:yyyy-MM-dd HH:mm:ss} UTC");
                }
                break;

            case AutoReplyDiffType.Disabled:
                sb.AppendLine("  Status: - Turn Off Auto-Reply");
                if (CurrentAutoReply != null && !string.IsNullOrWhiteSpace(CurrentAutoReply.ResponseSubject))
                {
                    sb.AppendLine($"    Previous Subject: '{CurrentAutoReply.ResponseSubject}'");
                }
                break;

            case AutoReplyDiffType.Modified:
                sb.AppendLine($"  Status: ~ Update Auto-Reply ({FieldDifferences.Count} change(s)):");
                foreach (var field in FieldDifferences)
                {
                    sb.AppendLine($"    ~ {field}");
                }
                break;
        }

        return sb.ToString().TrimEnd();
    }

    public override string ToString()
    {
        return DiffType switch
        {
            AutoReplyDiffType.Added => "+ AutoReply (Enable)",
            AutoReplyDiffType.Disabled => "- AutoReply (Disable)",
            AutoReplyDiffType.Modified => $"~ AutoReply ({FieldDifferences.Count} change(s): {string.Join(", ", FieldDifferences)})",
            _ => "  AutoReply (Unchanged)"
        };
    }
}
