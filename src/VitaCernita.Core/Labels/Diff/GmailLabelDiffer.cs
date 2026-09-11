using System;
using System.Collections.Generic;
using System.Linq;

namespace VitaCernita.Core.Labels.Diff;

/// <summary>
/// Provides diffing capabilities between existing Gmail labels (e.g. from the Gmail API)
/// and desired Gmail label specifications (e.g. from Lua configuration).
/// </summary>
public static class GmailLabelDiffer
{
    /// <summary>
    /// Computes the difference between a single current label and a desired label specification.
    /// </summary>
    public static LabelDiff Diff(GmailLabel? current, GmailLabel? desired, LabelDiffOptions? options = null)
    {
        options ??= new LabelDiffOptions();

        if (current == null && desired == null)
        {
            return new LabelDiff(string.Empty, null, LabelDiffType.Unchanged, null, null);
        }

        if (current == null)
        {
            return new LabelDiff(desired!.Name, desired.Id, LabelDiffType.Added, null, desired);
        }

        if (desired == null)
        {
            return new LabelDiff(current.Name, current.Id, LabelDiffType.Removed, current, null);
        }

        var fieldDiffs = new List<LabelFieldDiff>();

        // 1. Name
        if (options.ShouldCompareField("name"))
        {
            bool nameMatch = options.CaseInsensitiveNameMatch
                ? string.Equals(current.Name, desired.Name, StringComparison.OrdinalIgnoreCase)
                : string.Equals(current.Name, desired.Name, StringComparison.Ordinal);

            if (!nameMatch)
            {
                fieldDiffs.Add(new LabelFieldDiff("name", current.Name, desired.Name));
            }
        }

        // 2. MessageListVisibility
        if (options.ShouldCompareField("messageListVisibility"))
        {
            if (!(options.IgnoreUnsetDesiredFields && desired.MessageListVisibility == null))
            {
                if (!string.Equals(current.MessageListVisibility, desired.MessageListVisibility, StringComparison.OrdinalIgnoreCase))
                {
                    fieldDiffs.Add(new LabelFieldDiff("messageListVisibility", current.MessageListVisibility, desired.MessageListVisibility));
                }
            }
        }

        // 3. LabelListVisibility
        if (options.ShouldCompareField("labelListVisibility"))
        {
            if (!(options.IgnoreUnsetDesiredFields && desired.LabelListVisibility == null))
            {
                if (!string.Equals(current.LabelListVisibility, desired.LabelListVisibility, StringComparison.OrdinalIgnoreCase))
                {
                    fieldDiffs.Add(new LabelFieldDiff("labelListVisibility", current.LabelListVisibility, desired.LabelListVisibility));
                }
            }
        }

        // 4. Color
        bool compareColor = options.ShouldCompareField("color");
        bool compareText = options.ShouldCompareField("textColor");
        bool compareBg = options.ShouldCompareField("backgroundColor");

        if (compareColor || compareText || compareBg)
        {
            if (!(options.IgnoreUnsetDesiredFields && desired.Color == null))
            {
                if (current.Color == null && desired.Color != null)
                {
                    fieldDiffs.Add(new LabelFieldDiff("color", null, desired.Color));
                }
                else if (current.Color != null && desired.Color == null)
                {
                    fieldDiffs.Add(new LabelFieldDiff("color", current.Color, null));
                }
                else if (current.Color != null && desired.Color != null)
                {
                    bool textDiff = compareText && !string.Equals(current.Color.TextColor, desired.Color.TextColor, StringComparison.OrdinalIgnoreCase);
                    bool bgDiff = compareBg && !string.Equals(current.Color.BackgroundColor, desired.Color.BackgroundColor, StringComparison.OrdinalIgnoreCase);
                    if (textDiff || bgDiff)
                    {
                        fieldDiffs.Add(new LabelFieldDiff("color", current.Color, desired.Color));
                    }
                }
            }
        }

        var diffType = fieldDiffs.Count > 0 ? LabelDiffType.Modified : LabelDiffType.Unchanged;
        string name = desired.Name ?? current.Name;
        string? id = current.Id ?? desired.Id;

        return new LabelDiff(name, id, diffType, current, desired, fieldDiffs);
    }

    /// <summary>
    /// Computes the differences between a set of current labels and a set of desired labels.
    /// </summary>
    public static LabelSetDiff DiffSets(
        IEnumerable<GmailLabel>? current,
        IEnumerable<GmailLabel>? desired,
        LabelDiffOptions? options = null)
    {
        options ??= new LabelDiffOptions();
        var currentList = (current ?? Array.Empty<GmailLabel>()).ToList();
        var desiredList = (desired ?? Array.Empty<GmailLabel>()).ToList();

        var matchedCurrent = new HashSet<GmailLabel>();
        var results = new List<LabelDiff>();

        foreach (var desiredLabel in desiredList)
        {
            GmailLabel? matched = FindMatch(desiredLabel, currentList, matchedCurrent, options);

            if (matched != null)
            {
                matchedCurrent.Add(matched);
                var diff = Diff(matched, desiredLabel, options);
                if (diff.DiffType != LabelDiffType.Unchanged || options.IncludeUnchanged)
                {
                    results.Add(diff);
                }
            }
            else
            {
                results.Add(Diff(null, desiredLabel, options));
            }
        }

        foreach (var currentLabel in currentList)
        {
            if (!matchedCurrent.Contains(currentLabel))
            {
                results.Add(Diff(currentLabel, null, options));
            }
        }

        return new LabelSetDiff(results);
    }

    private static GmailLabel? FindMatch(
        GmailLabel desired,
        List<GmailLabel> currentList,
        HashSet<GmailLabel> matchedCurrent,
        LabelDiffOptions options)
    {
        StringComparison nameComparison = options.CaseInsensitiveNameMatch
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        switch (options.MatchBy)
        {
            case LabelMatchKey.Id:
                if (!string.IsNullOrEmpty(desired.Id))
                {
                    return currentList.FirstOrDefault(c =>
                        !matchedCurrent.Contains(c) &&
                        string.Equals(c.Id, desired.Id, StringComparison.OrdinalIgnoreCase));
                }
                return null;

            case LabelMatchKey.IdThenName:
                if (!string.IsNullOrEmpty(desired.Id))
                {
                    var matchById = currentList.FirstOrDefault(c =>
                        !matchedCurrent.Contains(c) &&
                        string.Equals(c.Id, desired.Id, StringComparison.OrdinalIgnoreCase));
                    if (matchById != null) return matchById;
                }
                return currentList.FirstOrDefault(c =>
                    !matchedCurrent.Contains(c) &&
                    string.Equals(c.Name, desired.Name, nameComparison));

            case LabelMatchKey.Name:
            default:
                return currentList.FirstOrDefault(c =>
                    !matchedCurrent.Contains(c) &&
                    string.Equals(c.Name, desired.Name, nameComparison));
        }
    }

    /// <summary>
    /// Overload for comparing label dictionaries directly (e.g. from Lua or JSON deserialization).
    /// </summary>
    public static LabelDiff Diff(
        IReadOnlyDictionary<string, object?>? currentDict,
        IReadOnlyDictionary<string, object?>? desiredDict,
        LabelDiffOptions? options = null)
    {
        var current = currentDict != null ? GmailLabel.FromDictionary(currentDict) : null;
        var desired = desiredDict != null ? GmailLabel.FromDictionary(desiredDict) : null;
        return Diff(current, desired, options);
    }

    /// <summary>
    /// Overload for comparing collections of label dictionaries directly.
    /// </summary>
    public static LabelSetDiff DiffSets(
        IEnumerable<IReadOnlyDictionary<string, object?>>? currentDicts,
        IEnumerable<IReadOnlyDictionary<string, object?>>? desiredDicts,
        LabelDiffOptions? options = null)
    {
        var current = currentDicts?.Select(GmailLabel.FromDictionary);
        var desired = desiredDicts?.Select(GmailLabel.FromDictionary);
        return DiffSets(current, desired, options);
    }

    /// <summary>
    /// Compares the current labels from a Gmail API users.labels.list JSON response string against desired labels.
    /// Filters out system labels by default (onlyUserLabels = true).
    /// </summary>
    public static LabelSetDiff DiffApiListResponse(
        string currentLabelsJson,
        IEnumerable<GmailLabel>? desired,
        LabelDiffOptions? options = null,
        bool onlyUserLabels = true)
    {
        var current = GmailLabel.FromApiListResponse(currentLabelsJson, onlyUserLabels);
        return DiffSets(current, desired, options);
    }

    /// <summary>
    /// Compares the current labels from a Gmail API users.labels.list JSON response string against desired dictionaries.
    /// </summary>
    public static LabelSetDiff DiffApiListResponse(
        string currentLabelsJson,
        IEnumerable<IReadOnlyDictionary<string, object?>>? desiredDicts,
        LabelDiffOptions? options = null,
        bool onlyUserLabels = true)
    {
        var desired = desiredDicts?.Select(GmailLabel.FromDictionary);
        return DiffApiListResponse(currentLabelsJson, desired, options, onlyUserLabels);
    }

    /// <summary>
    /// Compares two single JSON strings representing individual Gmail labels.
    /// </summary>
    public static LabelDiff DiffJson(
        string? currentJson,
        string? desiredJson,
        LabelDiffOptions? options = null)
    {
        var current = !string.IsNullOrWhiteSpace(currentJson) ? GmailLabel.FromJson(currentJson) : null;
        var desired = !string.IsNullOrWhiteSpace(desiredJson) ? GmailLabel.FromJson(desiredJson) : null;
        return Diff(current, desired, options);
    }
}
