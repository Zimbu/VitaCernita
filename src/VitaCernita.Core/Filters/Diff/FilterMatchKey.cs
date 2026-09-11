namespace VitaCernita.Core.Filters.Diff;

/// <summary>
/// Strategy used to pair filters between current and desired configurations.
/// </summary>
public enum FilterMatchKey
{
    /// <summary>
    /// Match filters by their unique Gmail ID only.
    /// </summary>
    Id,

    /// <summary>
    /// Match filters by ID when present; otherwise match by canonical search query criteria.
    /// Recommended default for synchronizing configurations with live accounts.
    /// </summary>
    IdThenQuery,

    /// <summary>
    /// Match filters strictly by their canonical Gmail search query string.
    /// </summary>
    Query,

    /// <summary>
    /// Match filters by ID when present; otherwise match by descriptive name.
    /// </summary>
    IdThenName
}
