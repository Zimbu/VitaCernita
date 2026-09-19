using System.Collections.Generic;
using VitaCernita.Core.Sync;

namespace VitaCernita.Core.Operations;

/// <summary>
/// Represents an executable Gmail REST API operation produced by translating a model diff.
/// Decouples model comparison from API execution mechanics and payload serialization.
/// </summary>
public interface IGmailOperation : ISyncCommand
{
    /// <summary>
    /// Alias for CommandId representing the unique operation identifier.
    /// </summary>
    string OperationId => CommandId;
}
