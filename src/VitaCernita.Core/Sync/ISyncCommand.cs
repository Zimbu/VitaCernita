using System.Collections.Generic;

namespace VitaCernita.Core.Sync;

/// <summary>
/// Represents an abstract, executable synchronization command to bring a target Gmail configuration
/// into alignment with a desired specification.
/// Designed for inspection in dry-run mode and subsequent automated dispatching.
/// </summary>
public interface ISyncCommand
{
    /// <summary>
    /// Unique identifier for this command instance.
    /// </summary>
    string CommandId { get; }

    /// <summary>
    /// The type of Gmail resource being synchronized (Label, Filter, AutoReply).
    /// </summary>
    SyncResourceType ResourceType { get; }

    /// <summary>
    /// The action being performed (Create, Update, Delete).
    /// </summary>
    SyncActionType ActionType { get; }

    /// <summary>
    /// Identifying name, query, or key of the target resource.
    /// </summary>
    string TargetIdentifier { get; }

    /// <summary>
    /// The Gmail server ID of the resource being updated or deleted (null when creating a new resource).
    /// </summary>
    string? TargetId { get; }

    /// <summary>
    /// Concise, single-line human-readable summary of the action.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Optional multi-line detailed description including field differences.
    /// </summary>
    string DetailedDescription { get; }

    /// <summary>
    /// JSON-ready payload dictionary for API execution (if applicable).
    /// </summary>
    Dictionary<string, object>? Payload { get; }

    /// <summary>
    /// Formats the command for dry-run inspection and reporting.
    /// </summary>
    string ToDryRunString();
}
