using System;
using System.Collections.Generic;
using System.Text;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.AutoReply.Diff;

namespace VitaCernita.Core.Sync.Commands;

/// <summary>
/// Command to update the Auto-Reply (Vacation Responder) settings in the target Gmail account.
/// Maps to PUT users.settings.vacation.
/// </summary>
public sealed class UpdateAutoReplyCommand : SyncCommandBase
{
    public AutoReply.AutoReply? CurrentAutoReply { get; }
    public AutoReply.AutoReply DesiredAutoReply { get; }
    public IReadOnlyList<AutoReplyFieldDiff> FieldDifferences { get; }
    public string? AccountError { get; }
    public bool IsEnabling { get; }
    public bool IsDisabling { get; }

    public UpdateAutoReplyCommand(
        AutoReply.AutoReply? currentAutoReply,
        AutoReply.AutoReply desiredAutoReply,
        IReadOnlyList<AutoReplyFieldDiff>? fieldDifferences = null,
        string? accountError = null,
        bool isEnabling = false,
        bool isDisabling = false,
        string? commandId = null)
        : base(
            commandId,
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
        FieldDifferences = fieldDifferences ?? Array.Empty<AutoReplyFieldDiff>();
        AccountError = accountError;
        IsEnabling = isEnabling;
        IsDisabling = isDisabling;
    }

    public static UpdateAutoReplyCommand FromDiff(AutoReplyDiff diff, string? commandId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));

        bool isEnabling = diff.DiffType == AutoReplyDiffType.Added;
        bool isDisabling = diff.DiffType == AutoReplyDiffType.Disabled;
        var desired = diff.DesiredAutoReply ?? new AutoReply.AutoReply { EnableAutoReply = false };

        return new UpdateAutoReplyCommand(
            diff.CurrentAutoReply,
            desired,
            diff.FieldDifferences,
            diff.AccountError,
            isEnabling,
            isDisabling,
            commandId);
    }

    private static string FormatDescription(
        AutoReply.AutoReply desired,
        bool isEnabling,
        bool isDisabling,
        IReadOnlyList<AutoReplyFieldDiff>? diffs,
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
        AutoReply.AutoReply desired,
        bool isEnabling,
        bool isDisabling,
        IReadOnlyList<AutoReplyFieldDiff>? diffs,
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
