using System;
using System.Collections.Generic;
using System.Text;
using AutoReplyModel = VitaCernita.Core.AutoReply.AutoReply;

namespace VitaCernita.Core.Diffing.AutoReply;

/// <summary>
/// Represents the computed difference between current and desired AutoReply (VacationSettings) configurations.
/// Pure model representation decoupled from Gmail API transport or payload mechanics.
/// </summary>
public sealed class AutoReplyDiff : ResourceDiff<AutoReplyModel>
{
    public AutoReplyModel? CurrentAutoReply => Current;
    public AutoReplyModel? DesiredAutoReply => Desired;
    public string? AccountError => Error;

    public AutoReplyDiff(
        DiffKind diffType,
        AutoReplyModel? currentAutoReply,
        AutoReplyModel? desiredAutoReply,
        IReadOnlyList<FieldDiff>? fieldDifferences = null,
        string? accountError = null)
        : base(diffType, currentAutoReply, desiredAutoReply, fieldDifferences, identifier: "AutoReply", id: null, error: accountError)
    {
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
            case DiffKind.Unchanged:
                sb.AppendLine("  Status: Unchanged");
                if (DesiredAutoReply != null && DesiredAutoReply.EnableAutoReply)
                {
                    sb.AppendLine($"  Active Subject: '{DesiredAutoReply.ResponseSubject}'");
                }
                break;

            case DiffKind.Added:
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

            case DiffKind.Disabled:
                sb.AppendLine("  Status: - Turn Off Auto-Reply");
                if (CurrentAutoReply != null && !string.IsNullOrWhiteSpace(CurrentAutoReply.ResponseSubject))
                {
                    sb.AppendLine($"    Previous Subject: '{CurrentAutoReply.ResponseSubject}'");
                }
                break;

            case DiffKind.Modified:
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
            DiffKind.Added => "+ AutoReply (Enable)",
            DiffKind.Disabled => "- AutoReply (Disable)",
            DiffKind.Modified => $"~ AutoReply ({FieldDifferences.Count} change(s): {string.Join(", ", FieldDifferences)})",
            _ => "  AutoReply (Unchanged)"
        };
    }
}
