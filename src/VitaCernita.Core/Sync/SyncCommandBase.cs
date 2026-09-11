using System;
using System.Collections.Generic;

namespace VitaCernita.Core.Sync;

/// <summary>
/// Common base class for synchronization commands.
/// </summary>
public abstract class SyncCommandBase : ISyncCommand
{
    public string CommandId { get; }
    public SyncResourceType ResourceType { get; }
    public SyncActionType ActionType { get; }
    public string TargetIdentifier { get; }
    public string? TargetId { get; }
    public string Description { get; }
    public string DetailedDescription { get; }
    public Dictionary<string, object>? Payload { get; }

    protected SyncCommandBase(
        string? commandId,
        SyncResourceType resourceType,
        SyncActionType actionType,
        string targetIdentifier,
        string? targetId,
        string description,
        string? detailedDescription = null,
        Dictionary<string, object>? payload = null)
    {
        CommandId = commandId ?? $"{resourceType.ToString().ToLowerInvariant()}-{actionType.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}";
        ResourceType = resourceType;
        ActionType = actionType;
        TargetIdentifier = targetIdentifier ?? throw new ArgumentNullException(nameof(targetIdentifier));
        TargetId = targetId;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        DetailedDescription = detailedDescription ?? description;
        Payload = payload;
    }

    public virtual string ToDryRunString()
    {
        string symbol = ActionType switch
        {
            SyncActionType.Create => "[+]",
            SyncActionType.Update => "[~]",
            SyncActionType.Delete => "[-]",
            _ => "[?]"
        };

        return $"{symbol} {Description}";
    }

    public override string ToString() => ToDryRunString();
}
