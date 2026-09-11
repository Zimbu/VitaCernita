namespace VitaCernita.Core.Sync;

/// <summary>
/// Specifies the action to be taken on a target resource during synchronization.
/// </summary>
public enum SyncActionType
{
    /// <summary>
    /// Create a new resource in the target account.
    /// </summary>
    Create,

    /// <summary>
    /// Update or edit an existing resource in the target account.
    /// </summary>
    Update,

    /// <summary>
    /// Delete an obsolete resource from the target account.
    /// </summary>
    Delete
}
