using System;
using System.Text;
using VitaCernita.Core.Sync;

namespace VitaCernita.Core.Operations.Labels;

/// <summary>
/// Gmail API operation to delete an obsolete label via DELETE users.labels.
/// </summary>
public class DeleteLabelOperation : GmailOperationBase
{
    public string LabelId => TargetId!;
    public string LabelName => TargetIdentifier;

    public DeleteLabelOperation(string labelId, string labelName, string? operationId = null)
        : base(
            operationId,
            SyncResourceType.Label,
            SyncActionType.Delete,
            targetIdentifier: labelName ?? throw new ArgumentNullException(nameof(labelName)),
            targetId: labelId ?? throw new ArgumentNullException(nameof(labelId)),
            description: $"Delete Label '{labelName}' (ID: {labelId})",
            detailedDescription: $"Delete Label: '{labelName}' (ID: {labelId})",
            payload: null)
    {
    }
}
