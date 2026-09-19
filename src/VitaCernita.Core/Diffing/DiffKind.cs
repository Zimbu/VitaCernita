namespace VitaCernita.Core.Diffing;

/// <summary>
/// Specifies the type of difference between the current state and desired state of a resource.
/// </summary>
public enum DiffKind
{
    /// <summary>
    /// Resource exists in both states and has identical properties.
    /// </summary>
    Unchanged,

    /// <summary>
    /// Resource does not exist in the current state and will be created.
    /// </summary>
    Added,

    /// <summary>
    /// Resource exists in the current state but is missing in the desired specification and will be removed.
    /// </summary>
    Removed,

    /// <summary>
    /// Resource exists in both states but has differences in one or more properties.
    /// </summary>
    Modified,

    /// <summary>
    /// Resource or setting is explicitly disabled.
    /// </summary>
    Disabled
}
