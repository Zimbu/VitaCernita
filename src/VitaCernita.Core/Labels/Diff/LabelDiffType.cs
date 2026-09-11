namespace VitaCernita.Core.Labels.Diff;

/// <summary>
/// Indicates the type of difference identified for a label.
/// </summary>
public enum LabelDiffType
{
    /// <summary>
    /// The label exists in both current and desired sets with identical configurable properties.
    /// </summary>
    Unchanged,

    /// <summary>
    /// The label exists in the desired set but not in the current set (action: create).
    /// </summary>
    Added,

    /// <summary>
    /// The label exists in the current set but not in the desired set (action: delete).
    /// </summary>
    Removed,

    /// <summary>
    /// The label exists in both sets but has differences in one or more configurable fields (action: patch/update).
    /// </summary>
    Modified
}
