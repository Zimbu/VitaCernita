using System.Collections.Generic;

namespace VitaCernita.Core.Diffing;

/// <summary>
/// Defines a contract for computing pure differences between current and desired resource states.
/// </summary>
/// <typeparam name="T">The resource type.</typeparam>
/// <typeparam name="TOptions">The comparison options type.</typeparam>
public interface IResourceDiffer<T, in TOptions>
{
    /// <summary>
    /// Computes the difference between a single current and desired instance.
    /// </summary>
    ResourceDiff<T> Diff(T? current, T? desired, TOptions? options = default);

    /// <summary>
    /// Computes differences between two collections of resources.
    /// </summary>
    ResourceSetDiff<T> DiffSets(IEnumerable<T> current, IEnumerable<T> desired, TOptions? options = default);
}
