using System;
using System.Collections.Generic;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Operations.AutoReply;

namespace VitaCernita.Core.Operations.Translators;

/// <summary>
/// Translates a pure model ResourceDiff&lt;AutoReply&gt; into executable Gmail REST API operations
/// and constructs Gmail-specific users.settings.vacation API payloads.
/// </summary>
public static class AutoReplyOperationTranslator
{
    /// <summary>
    /// Generates a payload suitable for PUT users.settings.updateVacation.
    /// </summary>
    public static Dictionary<string, object>? BuildUpdatePayload(ResourceDiff<Core.AutoReply.AutoReply> diff)
    {
        return diff?.Desired?.ToDictionary();
    }

    /// <summary>
    /// Translates an auto-reply diff into an UpdateAutoReplyOperation.
    /// </summary>
    public static UpdateAutoReplyOperation ToUpdateOperation(
        ResourceDiff<Core.AutoReply.AutoReply> diff,
        string? operationId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));

        bool isEnabling = diff.DiffType == DiffKind.Added;
        bool isDisabling = diff.DiffType == DiffKind.Disabled;
        var desired = diff.Desired ?? new Core.AutoReply.AutoReply { EnableAutoReply = false };

        return new UpdateAutoReplyOperation(
            diff.Current,
            desired,
            diff.FieldDifferences,
            diff.Error,
            isEnabling,
            isDisabling,
            operationId);
    }
}
