namespace VitaCernita.Core.Sync;

/// <summary>
/// Specifies the type of Gmail resource targeted by a synchronization command.
/// </summary>
public enum SyncResourceType
{
    /// <summary>
    /// A user label (users.labels).
    /// </summary>
    Label,

    /// <summary>
    /// A search filter (users.settings.filters).
    /// </summary>
    Filter,

    /// <summary>
    /// Auto-reply / vacation responder settings (users.settings.vacation).
    /// </summary>
    AutoReply
}
