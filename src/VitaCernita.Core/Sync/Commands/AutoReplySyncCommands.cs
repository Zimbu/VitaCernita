using System;
using System.Collections.Generic;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Operations.AutoReply;
using VitaCernita.Core.Operations.Translators;

namespace VitaCernita.Core.Sync.Commands;

/// <summary>
/// Command to update the Auto-Reply (Vacation Responder) settings in the target Gmail account.
/// Maps to PUT users.settings.vacation.
/// </summary>
public sealed class UpdateAutoReplyCommand : UpdateAutoReplyOperation
{
    public UpdateAutoReplyCommand(
        Core.AutoReply.AutoReply? currentAutoReply,
        Core.AutoReply.AutoReply desiredAutoReply,
        IReadOnlyList<FieldDiff>? fieldDifferences = null,
        string? accountError = null,
        bool isEnabling = false,
        bool isDisabling = false,
        string? commandId = null)
        : base(currentAutoReply, desiredAutoReply, fieldDifferences, accountError, isEnabling, isDisabling, commandId)
    {
    }

    public static UpdateAutoReplyCommand FromDiff(ResourceDiff<Core.AutoReply.AutoReply> diff, string? commandId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));

        bool isEnabling = diff.DiffType == DiffKind.Added;
        bool isDisabling = diff.DiffType == DiffKind.Disabled;
        var desired = diff.Desired ?? new Core.AutoReply.AutoReply { EnableAutoReply = false };

        return new UpdateAutoReplyCommand(
            diff.Current,
            desired,
            diff.FieldDifferences,
            diff.Error,
            isEnabling,
            isDisabling,
            commandId);
    }
}
