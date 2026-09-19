using System;
using System.Collections.Generic;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Diffing.Labels;

/// <summary>
/// Represents the computed difference between a current Gmail label and a desired label specification.
/// Pure model representation decoupled from Gmail API network or serialization concerns.
/// </summary>
public sealed class LabelDiff : ResourceDiff<GmailLabel>
{
    public new string Name => Identifier ?? Desired?.Name ?? Current?.Name ?? string.Empty;
    public GmailLabel? CurrentLabel => Current;
    public GmailLabel? DesiredLabel => Desired;

    public LabelDiff(
        string name,
        string? id,
        DiffKind diffType,
        GmailLabel? currentLabel,
        GmailLabel? desiredLabel,
        IReadOnlyList<FieldDiff>? fieldDifferences = null)
        : base(diffType, currentLabel, desiredLabel, fieldDifferences, identifier: name, id: id)
    {
    }

    public override string ToString()
    {
        return DiffType switch
        {
            DiffKind.Added => $"+ Label '{Name}' (Create)",
            DiffKind.Removed => $"- Label '{Name}' (Delete, ID: {Id ?? "<none>"})",
            DiffKind.Modified => $"~ Label '{Name}' ({FieldDifferences.Count} change(s): {string.Join(", ", FieldDifferences)})",
            _ => $"Label '{Name}' (Unchanged)"
        };
    }
}
