using System;
using System.Collections.Generic;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Operations.Labels;
using VitaCernita.Core.Operations.Translators;

namespace VitaCernita.Core.Sync.Commands;

/// <summary>
/// Command to create a new Gmail label in the target account.
/// </summary>
public sealed class CreateLabelCommand : CreateLabelOperation
{
    public CreateLabelCommand(GmailLabel label, string? commandId = null)
        : base(label, commandId)
    {
    }

    public static CreateLabelCommand FromDiff(ResourceDiff<GmailLabel> diff, string? commandId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        if (diff.Desired == null) throw new InvalidOperationException("Cannot create label without a desired specification.");
        return new CreateLabelCommand(diff.Desired, commandId);
    }
}

/// <summary>
/// Command to update (patch) an existing Gmail label in the target account.
/// </summary>
public sealed class UpdateLabelCommand : PatchLabelOperation
{
    public UpdateLabelCommand(
        string labelId,
        GmailLabel currentLabel,
        GmailLabel desiredLabel,
        IReadOnlyList<FieldDiff> fieldDifferences,
        Dictionary<string, object>? patchPayload = null,
        string? commandId = null)
        : base(labelId, currentLabel, desiredLabel, fieldDifferences, patchPayload, commandId)
    {
    }

    public static UpdateLabelCommand FromDiff(ResourceDiff<GmailLabel> diff, string? commandId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string name = diff.Identifier ?? diff.Desired?.Name ?? diff.Current?.Name ?? string.Empty;
        string id = diff.Id ?? diff.Current?.Id ?? throw new InvalidOperationException($"Cannot update label '{name}' without a valid ID.");
        return new UpdateLabelCommand(
            id,
            diff.Current ?? new GmailLabel(name),
            diff.Desired ?? throw new InvalidOperationException($"Cannot update label '{name}' without a desired specification."),
            diff.FieldDifferences,
            LabelOperationTranslator.BuildPatchPayload(diff),
            commandId);
    }
}

/// <summary>
/// Command to delete an obsolete Gmail label from the target account.
/// </summary>
public sealed class DeleteLabelCommand : DeleteLabelOperation
{
    public GmailLabel? CurrentLabel { get; }

    public DeleteLabelCommand(string labelId, string labelName, GmailLabel? currentLabel = null, string? commandId = null)
        : base(labelId, labelName, commandId)
    {
        CurrentLabel = currentLabel;
    }

    public static DeleteLabelCommand FromDiff(ResourceDiff<GmailLabel> diff, string? commandId = null)
    {
        if (diff == null) throw new ArgumentNullException(nameof(diff));
        string name = diff.Identifier ?? diff.Current?.Name ?? string.Empty;
        string id = LabelOperationTranslator.ResolveDeleteId(diff) ?? throw new InvalidOperationException($"Cannot delete label '{name}' without a valid ID.");
        return new DeleteLabelCommand(id, name, diff.Current, commandId);
    }
}
