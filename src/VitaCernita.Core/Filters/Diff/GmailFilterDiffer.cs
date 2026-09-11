using System;
using System.Collections.Generic;
using System.Linq;

namespace VitaCernita.Core.Filters.Diff;

/// <summary>
/// Provides diffing capabilities between existing Gmail filters (e.g. from the Gmail API)
/// and desired Gmail filter specifications (e.g. from Lua configuration).
/// </summary>
public static class GmailFilterDiffer
{
    /// <summary>
    /// Computes the difference between a single current filter and a desired filter specification.
    /// </summary>
    public static FilterDiff Diff(GmailFilter? current, GmailFilter? desired, FilterDiffOptions? options = null)
    {
        options ??= new FilterDiffOptions();

        if (current == null && desired == null)
        {
            return new FilterDiff(null, null, FilterDiffType.Unchanged, null, null);
        }

        if (current == null)
        {
            return new FilterDiff(desired!.Id, desired.Name, FilterDiffType.Added, null, desired);
        }

        if (desired == null)
        {
            return new FilterDiff(current.Id, current.Name, FilterDiffType.Removed, current, null);
        }

        var fieldDiffs = new List<FilterFieldDiff>();

        // 1. Query / Search criteria
        if (options.ShouldCompareField("query"))
        {
            string currentQuery = current.ToGmailQuery(explicitAnd: false);
            string desiredQuery = desired.ToGmailQuery(explicitAnd: false);

            if (!string.Equals(currentQuery, desiredQuery, StringComparison.Ordinal))
            {
                fieldDiffs.Add(new FilterFieldDiff("query", currentQuery, desiredQuery));
            }
        }

        // 2. Action
        if (options.ShouldCompareField("action"))
        {
            bool actionEqual = Equals(current.Action, desired.Action);
            if (!actionEqual)
            {
                string currentActStr = current.Action?.ToString() ?? "<none>";
                string desiredActStr = desired.Action?.ToString() ?? "<none>";
                fieldDiffs.Add(new FilterFieldDiff("action", currentActStr, desiredActStr));
            }
        }

        // 3. Name (optional comparison)
        if (options.CompareName && options.ShouldCompareField("name"))
        {
            if (!string.Equals(current.Name, desired.Name, StringComparison.OrdinalIgnoreCase))
            {
                fieldDiffs.Add(new FilterFieldDiff("name", current.Name, desired.Name));
            }
        }

        var diffType = fieldDiffs.Count > 0 ? FilterDiffType.Modified : FilterDiffType.Unchanged;
        string? id = current.Id ?? desired.Id;
        string? name = desired.Name ?? current.Name;

        return new FilterDiff(id, name, diffType, current, desired, fieldDiffs);
    }

    /// <summary>
    /// Computes the differences between a set of current filters and a set of desired filters.
    /// </summary>
    public static FilterSetDiff DiffSets(
        IEnumerable<GmailFilter>? current,
        IEnumerable<GmailFilter>? desired,
        FilterDiffOptions? options = null)
    {
        options ??= new FilterDiffOptions();
        var currentList = (current ?? Array.Empty<GmailFilter>()).ToList();
        var desiredList = (desired ?? Array.Empty<GmailFilter>()).ToList();

        var matchedCurrent = new HashSet<GmailFilter>();
        var results = new List<FilterDiff>();

        foreach (var desiredFilter in desiredList)
        {
            GmailFilter? matched = FindMatch(desiredFilter, currentList, matchedCurrent, options);

            if (matched != null)
            {
                matchedCurrent.Add(matched);
                var diff = Diff(matched, desiredFilter, options);
                if (diff.DiffType != FilterDiffType.Unchanged || options.IncludeUnchanged)
                {
                    results.Add(diff);
                }
            }
            else
            {
                results.Add(Diff(null, desiredFilter, options));
            }
        }

        foreach (var currentFilter in currentList)
        {
            if (!matchedCurrent.Contains(currentFilter))
            {
                results.Add(Diff(currentFilter, null, options));
            }
        }

        return new FilterSetDiff(results);
    }

    private static GmailFilter? FindMatch(
        GmailFilter desired,
        List<GmailFilter> currentList,
        HashSet<GmailFilter> matchedCurrent,
        FilterDiffOptions options)
    {
        switch (options.MatchBy)
        {
            case FilterMatchKey.Id:
                if (!string.IsNullOrEmpty(desired.Id))
                {
                    return currentList.FirstOrDefault(c =>
                        !matchedCurrent.Contains(c) &&
                        string.Equals(c.Id, desired.Id, StringComparison.OrdinalIgnoreCase));
                }
                return null;

            case FilterMatchKey.IdThenQuery:
                if (!string.IsNullOrEmpty(desired.Id))
                {
                    var matchById = currentList.FirstOrDefault(c =>
                        !matchedCurrent.Contains(c) &&
                        string.Equals(c.Id, desired.Id, StringComparison.OrdinalIgnoreCase));
                    if (matchById != null) return matchById;
                }
                string desiredQuery = desired.ToGmailQuery(explicitAnd: false);
                return currentList.FirstOrDefault(c =>
                    !matchedCurrent.Contains(c) &&
                    string.Equals(c.ToGmailQuery(explicitAnd: false), desiredQuery, StringComparison.Ordinal));

            case FilterMatchKey.Query:
                string q = desired.ToGmailQuery(explicitAnd: false);
                return currentList.FirstOrDefault(c =>
                    !matchedCurrent.Contains(c) &&
                    string.Equals(c.ToGmailQuery(explicitAnd: false), q, StringComparison.Ordinal));

            case FilterMatchKey.IdThenName:
                if (!string.IsNullOrEmpty(desired.Id))
                {
                    var matchById = currentList.FirstOrDefault(c =>
                        !matchedCurrent.Contains(c) &&
                        string.Equals(c.Id, desired.Id, StringComparison.OrdinalIgnoreCase));
                    if (matchById != null) return matchById;
                }
                if (!string.IsNullOrEmpty(desired.Name))
                {
                    return currentList.FirstOrDefault(c =>
                        !matchedCurrent.Contains(c) &&
                        string.Equals(c.Name, desired.Name, StringComparison.OrdinalIgnoreCase));
                }
                return null;

            default:
                return null;
        }
    }

    /// <summary>
    /// Overload for comparing filter dictionaries directly (e.g. from Lua or JSON deserialization).
    /// </summary>
    public static FilterDiff Diff(
        IReadOnlyDictionary<string, object?>? currentDict,
        IReadOnlyDictionary<string, object?>? desiredDict,
        FilterDiffOptions? options = null)
    {
        var current = currentDict != null ? GmailFilter.FromDictionary(currentDict) : null;
        var desired = desiredDict != null ? GmailFilter.FromDictionary(desiredDict) : null;
        return Diff(current, desired, options);
    }

    /// <summary>
    /// Overload for comparing collections of filter dictionaries directly.
    /// </summary>
    public static FilterSetDiff DiffSets(
        IEnumerable<IReadOnlyDictionary<string, object?>>? currentDicts,
        IEnumerable<IReadOnlyDictionary<string, object?>>? desiredDicts,
        FilterDiffOptions? options = null)
    {
        var current = currentDicts?.Select(GmailFilter.FromDictionary);
        var desired = desiredDicts?.Select(GmailFilter.FromDictionary);
        return DiffSets(current, desired, options);
    }

    /// <summary>
    /// Compares the current filters from a Gmail API users.settings.filters.list JSON response string against desired filters.
    /// </summary>
    public static FilterSetDiff DiffApiListResponse(
        string currentFiltersJson,
        IEnumerable<GmailFilter>? desired,
        FilterDiffOptions? options = null)
    {
        var current = GmailFilter.FromApiListResponse(currentFiltersJson);
        return DiffSets(current, desired, options);
    }

    /// <summary>
    /// Compares two single JSON strings representing individual Gmail filters.
    /// </summary>
    public static FilterDiff DiffJson(
        string? currentJson,
        string? desiredJson,
        FilterDiffOptions? options = null)
    {
        var current = !string.IsNullOrWhiteSpace(currentJson) ? GmailFilter.FromJson(currentJson) : null;
        var desired = !string.IsNullOrWhiteSpace(desiredJson) ? GmailFilter.FromJson(desiredJson) : null;
        return Diff(current, desired, options);
    }
}
