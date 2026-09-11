using System;
using System.Collections.Generic;

namespace VitaCernita.Core.Labels.Diff;

/// <summary>
/// Options controlling label comparison and field filtering.
/// </summary>
public sealed class LabelDiffOptions
{
    /// <summary>
    /// Strategy used to pair labels between the current and desired sets.
    /// </summary>
    public LabelMatchKey MatchBy { get; set; } = LabelMatchKey.Name;

    /// <summary>
    /// When matching by name, whether the comparison should ignore case differences. Default is true.
    /// </summary>
    public bool CaseInsensitiveNameMatch { get; set; } = true;

    /// <summary>
    /// If true, fields that are null/unset in the desired configuration are ignored and not treated as deletions/changes.
    /// Useful for partial patch scenarios.
    /// </summary>
    public bool IgnoreUnsetDesiredFields { get; set; } = false;

    /// <summary>
    /// Optional whitelist of field names to compare (e.g., 'messageListVisibility', 'color').
    /// If null or empty, all configurable fields are compared.
    /// </summary>
    public IReadOnlySet<string>? FieldsToCompare { get; set; }

    /// <summary>
    /// Optional blacklist of field names to ignore during comparison.
    /// </summary>
    public IReadOnlySet<string>? FieldsToIgnore { get; set; }

    /// <summary>
    /// Whether to include unchanged labels in the diff result collection. Default is true.
    /// </summary>
    public bool IncludeUnchanged { get; set; } = true;

    /// <summary>
    /// Determines whether a given configurable field should be compared based on the active options.
    /// </summary>
    public bool ShouldCompareField(string fieldName)
    {
        string canonical = CanonicalizeFieldName(fieldName);

        if (FieldsToIgnore != null)
        {
            foreach (var ignored in FieldsToIgnore)
            {
                string ignoredCanonical = CanonicalizeFieldName(ignored);
                if (string.Equals(ignoredCanonical, canonical, StringComparison.OrdinalIgnoreCase) ||
                    (ignoredCanonical == "color" && canonical.StartsWith("color.", StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }
        }

        if (FieldsToCompare != null && FieldsToCompare.Count > 0)
        {
            foreach (var allowed in FieldsToCompare)
            {
                string allowedCanonical = CanonicalizeFieldName(allowed);
                if (string.Equals(allowedCanonical, canonical, StringComparison.OrdinalIgnoreCase) ||
                    (allowedCanonical == "color" && canonical.StartsWith("color.", StringComparison.OrdinalIgnoreCase)) ||
                    (canonical == "color" && (allowedCanonical == "color.textcolor" || allowedCanonical == "color.backgroundcolor")))
                {
                    return true;
                }
            }
            return false;
        }

        return true;
    }

    private static string CanonicalizeFieldName(string name)
    {
        string lower = name.Trim().ToLowerInvariant().Replace("_", "");
        return lower switch
        {
            "name" => "name",
            "messagelistvisibility" => "messagelistvisibility",
            "labellistvisibility" => "labellistvisibility",
            "color" => "color",
            "textcolor" => "color.textcolor",
            "backgroundcolor" => "color.backgroundcolor",
            _ => lower
        };
    }
}
