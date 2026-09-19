using System;
using System.Collections.Generic;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using AutoReplyModel = VitaCernita.Core.AutoReply.AutoReply;

namespace VitaCernita.Core.Diffing;

/// <summary>
/// Represents the computed difference between a current state and a desired state for a resource of type T.
/// Completely decoupled from transport protocols, API endpoints, or JSON payload representations.
/// </summary>
/// <typeparam name="T">The resource entity type.</typeparam>
public class ResourceDiff<T>
{
    /// <summary>
    /// Human-readable identifier or name of the resource (e.g. label name, filter query summary).
    /// </summary>
    public string? Identifier { get; }

    /// <summary>
    /// Alias for Identifier to support name-centric resources.
    /// </summary>
    public string? Name => Identifier;

    /// <summary>
    /// Server/provider-assigned ID of the resource (null if new or unassigned).
    /// </summary>
    public string? Id { get; }

    /// <summary>
    /// The classified kind of difference (Added, Removed, Modified, Unchanged, Disabled).
    /// </summary>
    public DiffKind DiffType { get; }

    /// <summary>
    /// Current state from the remote/server (null if Added).
    /// </summary>
    public T? Current { get; }

    /// <summary>
    /// Desired state from the local configuration (null if Removed).
    /// </summary>
    public T? Desired { get; }

    /// <summary>
    /// Granular list of field-level differences.
    /// </summary>
    public IReadOnlyList<FieldDiff> FieldDifferences { get; }

    /// <summary>
    /// Optional account- or resource-level error message encountered during comparison.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Alias for Error when diffing account settings like AutoReply.
    /// </summary>
    public string? AccountError => Error;

    /// <summary>
    /// True if there is a divergence between current and desired state.
    /// </summary>
    public bool HasChanges => DiffType != DiffKind.Unchanged;

    public ResourceDiff(
        DiffKind diffType,
        T? current,
        T? desired,
        IReadOnlyList<FieldDiff>? fieldDifferences = null,
        string? identifier = null,
        string? id = null,
        string? error = null)
    {
        DiffType = diffType;
        Current = current;
        Desired = desired;
        FieldDifferences = fieldDifferences ?? Array.Empty<FieldDiff>();
        Identifier = identifier;
        Id = id;
        Error = error;
    }

    public override string ToString()
    {
        if (typeof(T) == typeof(GmailLabel))
        {
            string labelName = Identifier ?? (Current as GmailLabel)?.Name ?? (Desired as GmailLabel)?.Name ?? string.Empty;
            return DiffType switch
            {
                DiffKind.Added => $"+ Label '{labelName}' (Create)",
                DiffKind.Removed => $"- Label '{labelName}' (Delete, ID: {Id ?? "<none>"})",
                DiffKind.Modified => $"~ Label '{labelName}' ({FieldDifferences.Count} change(s): {string.Join(", ", FieldDifferences)})",
                _ => $"Label '{labelName}' (Unchanged)"
            };
        }

        if (typeof(T) == typeof(GmailFilter))
        {
            string idInfo = Id != null ? $" (ID: {Id})" : "";
            string nameInfo = !string.IsNullOrWhiteSpace(Identifier) ? $" '{Identifier}'" : "";
            var desiredFilter = Desired as GmailFilter;

            return DiffType switch
            {
                DiffKind.Added => $"+ Filter{nameInfo}{idInfo} (Create): query='{desiredFilter?.ToGmailQuery()}' action={desiredFilter?.Action}",
                DiffKind.Removed => $"- Filter{nameInfo}{idInfo} (Delete)",
                DiffKind.Modified => $"~ Filter{nameInfo}{idInfo} ({FieldDifferences.Count} change(s): {string.Join(", ", FieldDifferences)})",
                _ => $"Filter{nameInfo}{idInfo} (Unchanged)"
            };
        }

        if (typeof(T) == typeof(AutoReplyModel))
        {
            return DiffType switch
            {
                DiffKind.Added => "+ AutoReply (Enable)",
                DiffKind.Disabled => "- AutoReply (Disable)",
                DiffKind.Modified => $"~ AutoReply ({FieldDifferences.Count} change(s): {string.Join(", ", FieldDifferences)})",
                _ => "  AutoReply (Unchanged)"
            };
        }

        string idPart = Id != null ? $" (ID: {Id})" : "";
        string namePart = !string.IsNullOrWhiteSpace(Identifier) ? $" '{Identifier}'" : "";
        string typeName = typeof(T).Name;

        return DiffType switch
        {
            DiffKind.Added => $"+ {typeName}{namePart}{idPart} (Added)",
            DiffKind.Removed => $"- {typeName}{namePart}{idPart} (Removed)",
            DiffKind.Modified => $"~ {typeName}{namePart}{idPart} ({FieldDifferences.Count} change(s): {string.Join(", ", FieldDifferences)})",
            DiffKind.Disabled => $"- {typeName}{namePart}{idPart} (Disabled)",
            _ => $"{typeName}{namePart}{idPart} (Unchanged)"
        };
    }
}
