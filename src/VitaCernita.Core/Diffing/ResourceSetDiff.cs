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

    public string ToSummaryString()
    {
        return $"Summary: {TotalCreations} to create, {TotalModifications} to update, {TotalDeletions} to delete, {TotalUnchanged} unchanged.";
    }

    public override string ToString() => ToSummaryString();
}
