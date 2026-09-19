using System;
using System.Collections.Generic;
using System.Text;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Sync;

namespace VitaCernita.Core.Operations.Filters;

/// <summary>
/// Gmail API operation to create a new search filter via POST users.settings.filters.
/// </summary>
public class CreateFilterOperation : GmailOperationBase
{
    public GmailFilter Filter { get; }
    public string? FilterName => Filter.Name;

    public CreateFilterOperation(GmailFilter filter, string? operationId = null, IEnumerable<GmailLabel>? knownLabels = null)
        : base(
            operationId,
            SyncResourceType.Filter,
            SyncActionType.Create,
            targetIdentifier: !string.IsNullOrWhiteSpace(filter?.Name) ? filter.Name : (filter?.ToGmailQuery() ?? "<filter>"),
            targetId: null,
            description: FormatDescription(filter!),
            detailedDescription: FormatDetailedDescription(filter!),
            payload: filter?.ToDictionary(knownLabels))
    {
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    private static string FormatDescription(GmailFilter filter)
    {
        string nameInfo = !string.IsNullOrWhiteSpace(filter.Name) ? $" '{filter.Name}'" : "";
        string queryStr = filter.ToGmailQuery();
        string actionStr = filter.Action?.ToString() ?? "<none>";
        return $"Create Filter{nameInfo}: query '{queryStr}' -> action {actionStr}";
    }

    private static string FormatDetailedDescription(GmailFilter filter)
    {
        var sb = new StringBuilder();
        string nameInfo = !string.IsNullOrWhiteSpace(filter.Name) ? $"Name: '{filter.Name}'" : "Filter";
        sb.AppendLine($"Create {nameInfo}");
        sb.AppendLine($"  Query : {filter.ToGmailQuery()}");
        sb.AppendLine($"  Action: {filter.Action?.ToString() ?? "<none>"}");
        return sb.ToString().TrimEnd();
    }
}
