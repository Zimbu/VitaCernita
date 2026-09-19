using System;
using System.Text;
using VitaCernita.Core.Sync;

namespace VitaCernita.Core.Operations.Filters;

/// <summary>
/// Gmail API operation to delete an obsolete filter via DELETE users.settings.filters.
/// </summary>
public class DeleteFilterOperation : GmailOperationBase
{
    public string FilterId => TargetId!;
    public string? FilterName => TargetIdentifier != FilterId ? TargetIdentifier : null;

    public DeleteFilterOperation(string filterId, string? filterName = null, string? operationId = null)
        : base(
            operationId,
            SyncResourceType.Filter,
            SyncActionType.Delete,
            targetIdentifier: !string.IsNullOrWhiteSpace(filterName) ? filterName : (filterId ?? throw new ArgumentNullException(nameof(filterId))),
            targetId: filterId ?? throw new ArgumentNullException(nameof(filterId)),
            description: $"Delete Filter (ID: {filterId})" + (!string.IsNullOrWhiteSpace(filterName) ? $" '{filterName}'" : ""),
            detailedDescription: $"Delete Filter (ID: {filterId})" + (!string.IsNullOrWhiteSpace(filterName) ? $" '{filterName}'" : ""),
            payload: null)
    {
    }
}
