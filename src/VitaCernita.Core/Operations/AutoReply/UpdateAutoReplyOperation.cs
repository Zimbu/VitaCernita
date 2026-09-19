using System;
using System.Collections.Generic;
using System.Text;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Sync;

namespace VitaCernita.Core.Operations.AutoReply;

/// <summary>
/// Gmail API operation to update the Auto-Reply (Vacation Responder) settings via PUT users.settings.vacation.
/// </summary>
public class UpdateAutoReplyOperation : GmailOperationBase
{
    public Core.AutoReply.AutoReply? CurrentAutoReply { get; }
    public Core.AutoReply.AutoReply DesiredAutoReply { get; }
    public IReadOnlyList<FieldDiff> FieldDifferences { get; }
    public string? AccountError { get; }
    public bool IsEnabling { get; }
    public bool IsDisabling { get; }

    public UpdateAutoReplyOperation(
        Core.AutoReply.AutoReply? currentAutoReply,
        Core.AutoReply.AutoReply desiredAutoReply,
        IReadOnlyList<FieldDiff>? fieldDifferences = null,
        string? accountError = null,
        bool isEnabling = false,
        bool isDisabling = false,
        string? operationId = null)
        : base(
            operationId,
            SyncResourceType.AutoReply,
            isDisabling ? SyncActionType.Delete : (isEnabling ? SyncActionType.Create : SyncActionType.Update),
            targetIdentifier: "AutoReply",
            targetId: null,
            description: FormatDescription(desiredAutoReply, isEnabling, isDisabling, fieldDifferences, accountError),
            detailedDescription: FormatDetailedDescription(desiredAutoReply, isEnabling, isDisabling, fieldDifferences, accountError),
            payload: desiredAutoReply?.ToDictionary())
    {
        CurrentAutoReply = currentAutoReply;
        DesiredAutoReply = desiredAutoReply ?? throw new ArgumentNullException(nameof(desiredAutoReply));
        FieldDifferences = fieldDifferences ?? Array.Empty<FieldDiff>();
        AccountError = accountError;
        IsEnabling = isEnabling;
        IsDisabling = isDisabling;
    }

    private static string FormatDescription(
        Core.AutoReply.AutoReply desired,
        bool isEnabling,
        bool isDisabling,
        IReadOnlyList<FieldDiff>? diffs,
        string? error)
    {
        string errorPrefix = error != null ? "[ERROR: Incompatible Domain Restriction] " : "";

        if (isDisabling)
        {
            return $"{errorPrefix}Disable Auto-Reply (turn off vacation responder)";
        }

        if (isEnabling)
        {
            return $"{errorPrefix}Enable Auto-Reply: subject '{desired.ResponseSubject}'";
        }

        int count = diffs?.Count ?? 0;
        return $"{errorPrefix}Update Auto-Reply: {count} setting(s) changed";
    }

    private static string FormatDetailedDescription(
        Core.AutoReply.AutoReply desired,
        bool isEnabling,
        bool isDisabling,
        IReadOnlyList<FieldDiff>? diffs,
        string? error)
    {
        var sb = new StringBuilder();
        if (error != null)
        {
            sb.AppendLine($"  [ERROR] {error}");
        }

        if (isDisabling)
        {
            sb.AppendLine("Disable Auto-Reply (turn off vacation responder)");
            return sb.ToString().TrimEnd();
        }

        if (isEnabling)
        {
            sb.AppendLine("Enable Auto-Reply:");
            sb.AppendLine($"  Subject     : '{desired.ResponseSubject}'");
            if (!string.IsNullOrWhiteSpace(desired.ResponseBodyPlainText))
                sb.AppendLine($"  Body (Plain): '{desired.ResponseBodyPlainText.Replace("\n", " ")}'");
            if (!string.IsNullOrWhiteSpace(desired.ResponseBodyHtml))
                sb.AppendLine($"  Body (HTML) : '{desired.ResponseBodyHtml.Replace("\n", " ")}'");
            sb.AppendLine($"  ContactsOnly: {desired.RestrictToContacts}");
            sb.AppendLine($"  DomainOnly  : {desired.RestrictToDomain}");
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine("Update Auto-Reply:");
        if (diffs != null)
        {
            foreach (var d in diffs)
            {
                sb.AppendLine($"  ~ {d}");
            }
        }
        return sb.ToString().TrimEnd();
    }
}
