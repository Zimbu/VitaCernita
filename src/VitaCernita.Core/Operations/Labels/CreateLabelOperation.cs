using System;
using System.Collections.Generic;
using System.Text;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Sync;

namespace VitaCernita.Core.Operations.Labels;

/// <summary>
/// Gmail API operation to create a new label via POST users.labels.
/// </summary>
public class CreateLabelOperation : GmailOperationBase
{
    public GmailLabel Label { get; }
    public string LabelName => TargetIdentifier;

    public CreateLabelOperation(GmailLabel label, string? operationId = null)
        : base(
            operationId,
            SyncResourceType.Label,
            SyncActionType.Create,
            targetIdentifier: label?.Name ?? throw new ArgumentNullException(nameof(label)),
            targetId: null,
            description: FormatDescription(label),
            detailedDescription: FormatDetailedDescription(label),
            payload: label.ToDictionary())
    {
        Label = label;
    }

    private static string FormatDescription(GmailLabel label)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(label.MessageListVisibility))
            details.Add($"MessageList: {label.MessageListVisibility}");
        if (!string.IsNullOrWhiteSpace(label.LabelListVisibility))
            details.Add($"LabelList: {label.LabelListVisibility}");
        if (label.Color != null)
            details.Add($"Color: [{label.Color.TextColor} / {label.Color.BackgroundColor}]");

        string extra = details.Count > 0 ? $" ({string.Join(", ", details)})" : "";
        return $"Create Label '{label.Name}'{extra}";
    }

    private static string FormatDetailedDescription(GmailLabel label)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Create Label: '{label.Name}'");
        if (label.MessageListVisibility != null) sb.AppendLine($"  MessageListVisibility: {label.MessageListVisibility}");
        if (label.LabelListVisibility != null) sb.AppendLine($"  LabelListVisibility: {label.LabelListVisibility}");
        if (label.Color != null) sb.AppendLine($"  Color: Text={label.Color.TextColor}, Bg={label.Color.BackgroundColor}");
        return sb.ToString().TrimEnd();
    }
}
