namespace VitaCernita.Core.Sync;

/// <summary>
/// Controls the synchronization direction between two Gmail sources (left and right).
/// </summary>
public enum SyncDirection
{
    /// <summary>
    /// Commands are generated to mutate the Right target source so that it matches the Left reference source.
    /// This is the primary mode: Left is desired state (e.g. local Lua configuration), Right is target state (e.g. live Gmail account).
    /// </summary>
    MakeRightMatchLeft,

    /// <summary>
    /// Commands are generated to mutate the Left target source so that it matches the Right reference source.
    /// </summary>
    MakeLeftMatchRight
}
