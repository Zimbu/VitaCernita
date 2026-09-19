using System.Collections.Generic;
using VitaCernita.Core.Sync;

namespace VitaCernita.Core.Operations;

/// <summary>
/// Base class for all concrete Gmail API operations.
/// </summary>
public abstract class GmailOperationBase : SyncCommandBase, IGmailOperation
{
    protected GmailOperationBase(
        string? operationId,
        SyncResourceType resourceType,
        SyncActionType actionType,
        string targetIdentifier,
        string? targetId,
        string description,
        string detailedDescription,
        Dictionary<string, object>? payload)
        : base(operationId, resourceType, actionType, targetIdentifier, targetId, description, detailedDescription, payload)
    {
    }
}
