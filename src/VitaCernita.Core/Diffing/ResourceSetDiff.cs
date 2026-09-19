using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VitaCernita.Core.Diffing;

/// <summary>
/// Represents the aggregate differences between two collections of resources of type T.
/// Partitions changes into additions, modifications, removals, and unchanged items.
/// </summary>
/// <typeparam name="T">The resource entity type.</typeparam>
public class ResourceSetDiff<T>
{
    public IReadOnlyList<ResourceDiff<T>> Differences { get; }

    public IReadOnlyList<ResourceDiff<T>> Creations =>
        Differences.Where(d => d.DiffType == DiffKind.Added).ToList();

    public IReadOnlyList<ResourceDiff<T>> Deletions =>
        Differences.Where(d => d.DiffType == DiffKind.Removed).ToList();

    public IReadOnlyList<ResourceDiff<T>> Modifications =>
        Differences.Where(d => d.DiffType == DiffKind.Modified).ToList();

    public IReadOnlyList<ResourceDiff<T>> Unchanged =>
        Differences.Where(d => d.DiffType == DiffKind.Unchanged).ToList();

    public int TotalCreations => Creations.Count;
    public int TotalModifications => Modifications.Count;
    public int TotalDeletions => Deletions.Count;
    public int TotalUnchanged => Unchanged.Count;
    public int TotalDifferences => Differences.Count;

    public bool HasDifferences => Differences.Any(d => d.HasChanges);

    public ResourceSetDiff(IEnumerable<ResourceDiff<T>> differences)
    {
        Differences = (differences ?? Array.Empty<ResourceDiff<T>>()).ToList();
    }

    /// <summary>
    /// Generates a standardized, human-readable summary of all changes suitable for CLI dry-run output.
    /// </summary>
    public virtual string ToDryRunReport(string resourceTitle = "Resource")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"VitaCernita {resourceTitle} Diff Report (Dry Run):");
        sb.AppendLine($"  [+] Create ({TotalCreations})");
        foreach (var c in Creations)
        {
            sb.AppendLine($"      + {c.Identifier ?? c.Id ?? "<item>"}");
        }

        sb.AppendLine($"  [~] Update ({TotalModifications})");
        foreach (var m in Modifications)
        {
            string fieldSummary = m.FieldDifferences.Count > 0
                ? $" ({m.FieldDifferences.Count} field(s) changed: {string.Join(", ", m.FieldDifferences.Select(f => f.FieldName))})"
                : "";
            sb.AppendLine($"      ~ {m.Identifier ?? m.Id ?? "<item>"}{fieldSummary}");
        }

        sb.AppendLine($"  [-] Delete ({TotalDeletions})");
        foreach (var d in Deletions)
        {
            sb.AppendLine($"      - {d.Identifier ?? d.Id ?? "<item>"}");
        }

        sb.AppendLine($"  [=] Unchanged ({TotalUnchanged})");
        return sb.ToString().TrimEnd();
    }
}
