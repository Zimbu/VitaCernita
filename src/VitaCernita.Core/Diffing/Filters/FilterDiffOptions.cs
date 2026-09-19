using System;
using System.Collections.Generic;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Diffing.Filters;

/// <summary>
/// Options controlling filter comparison, matching strategy, and field filtering.
/// </summary>
public sealed class FilterDiffOptions
{
    /// <summary>
    /// Strategy used to pair filters between the current and desired configurations.
    /// Default is <see cref="FilterMatchKey.IdThenQuery"/>.
    /// </summary>
    public FilterMatchKey MatchBy { get; set; } = FilterMatchKey.IdThenQuery;

    /// <summary>
    /// Whether to include unchanged filters in the diff result collection. Default is true.
    /// </summary>
    public bool IncludeUnchanged { get; set; } = true;

    /// <summary>
    /// Whether to compare the optional human-readable name of filters. Default is false,
    /// because Gmail API filters are anonymous server-side and only identify by ID.
    /// </summary>
    public bool CompareName { get; set; } = false;

    /// <summary>
    /// Optional whitelist of field names to compare (e.g., 'query', 'action').
    /// If null or empty, all configurable fields are compared.
    /// </summary>
    public IReadOnlySet<string>? FieldsToCompare { get; set; }

    /// <summary>
    /// Optional blacklist of field names to ignore during comparison.
    /// </summary>
    public IReadOnlySet<string>? FieldsToIgnore { get; set; }

    /// <summary>
    /// Known Gmail labels used to resolve internal label IDs to human-readable label names (and vice-versa)
    /// during filter action comparison.
    /// </summary>
    public IEnumerable<GmailLabel>? KnownLabels { get; set; }

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
                    (ignoredCanonical == "action" && canonical.StartsWith("action.", StringComparison.OrdinalIgnoreCase)))
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
                    (allowedCanonical == "action" && canonical.StartsWith("action.", StringComparison.OrdinalIgnoreCase)))
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
            "criteria" => "query",
            "condition" => "query",
            _ => lower
        };
    }
}
