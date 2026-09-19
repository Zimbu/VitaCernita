using System.Collections.Generic;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Operations.Translators;

/// <summary>
/// Extension methods bridging pure model diffs to Gmail API payload generation.
/// Enables convenient API payload resolution without coupling the diff model itself to Gmail REST details.
/// </summary>
public static class OperationExtensions
{
    /// <summary>
    /// Generates a patch dictionary payload for a Gmail API users.labels.patch call.
    /// </summary>
    public static Dictionary<string, object> GetPatchPayload(this ResourceDiff<GmailLabel> diff) =>
        LabelOperationTranslator.BuildPatchPayload(diff);

    /// <summary>
    /// Generates a create dictionary payload for a Gmail API users.labels.create call.
    /// </summary>
    public static Dictionary<string, object>? GetCreatePayload(this ResourceDiff<GmailLabel> diff) =>
        LabelOperationTranslator.BuildCreatePayload(diff);

    /// <summary>
    /// Resolves the label ID to delete for a Gmail API users.labels.delete call.
    /// </summary>
    public static string? GetDeleteId(this ResourceDiff<GmailLabel> diff) =>
        LabelOperationTranslator.ResolveDeleteId(diff);

    /// <summary>
    /// Generates a create dictionary payload for a Gmail API users.settings.filters.create call.
    /// </summary>
    public static Dictionary<string, object>? GetCreatePayload(
        this ResourceDiff<GmailFilter> diff,
        bool explicitAnd = false,
        IEnumerable<GmailLabel>? knownLabels = null) =>
        FilterOperationTranslator.BuildCreatePayload(diff, explicitAnd, knownLabels);

    /// <summary>
    /// Resolves the filter ID to delete for a Gmail API users.settings.filters.delete call.
    /// </summary>
    public static string? GetDeleteId(this ResourceDiff<GmailFilter> diff) =>
        FilterOperationTranslator.ResolveDeleteId(diff);

    /// <summary>
    /// Generates an update dictionary payload for a Gmail API users.settings.updateVacation call.
    /// </summary>
    public static Dictionary<string, object>? GetUpdatePayload(this ResourceDiff<Core.AutoReply.AutoReply> diff) =>
        AutoReplyOperationTranslator.BuildUpdatePayload(diff);
}
