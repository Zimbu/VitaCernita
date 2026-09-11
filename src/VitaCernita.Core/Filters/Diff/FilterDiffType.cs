namespace VitaCernita.Core.Filters.Diff;

/// <summary>
/// Indicates the difference status of a Gmail filter between current and desired configurations.
/// </summary>
public enum FilterDiffType
{
    /// <summary>
    /// Filter exists in both configurations with identical query criteria and actions.
    /// </summary>
    Unchanged,

    /// <summary>
    /// Filter exists in the desired configuration but is missing in the current configuration (requires creation).
    /// </summary>
    Added,

    /// <summary>
    /// Filter exists in the current configuration but is missing in the desired configuration (requires deletion).
    /// </summary>
    Removed,

    /// <summary>
    /// Filter exists in both configurations but has altered query criteria or action specifications (requires update).
    /// </summary>
    Modified
}
