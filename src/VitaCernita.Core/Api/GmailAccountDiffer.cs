using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Labels.Diff;

namespace VitaCernita.Core.Api;

/// <summary>
/// Coordinates diffing local Lua label configurations against a target Gmail account via IGmailApiClient.
/// </summary>
public static class GmailAccountDiffer
{
    /// <summary>
    /// Compares desired labels against the target Gmail account.
    /// </summary>
    public static async Task<LabelSetDiff> DiffLabelsAsync(
        IGmailApiClient client,
        IEnumerable<GmailLabel> desiredLabels,
        LabelDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));

        var currentLabels = await client.ListLabelsAsync(userId, onlyUserLabels: true, cancellationToken);
        return GmailLabelDiffer.DiffSets(currentLabels, desiredLabels, options);
    }

    /// <summary>
    /// Loads labels from a Lua script and compares them against the target Gmail account.
    /// </summary>
    public static async Task<LabelSetDiff> DiffLabelsFromScriptAsync(
        IGmailApiClient client,
        string luaScript,
        LabelDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        var loader = new GmailLabelLoader();
        var desiredLabels = await loader.LoadLabelsFromScriptAsync(luaScript);
        return await DiffLabelsAsync(client, desiredLabels, options, userId, cancellationToken);
    }

    /// <summary>
    /// Loads labels from a Lua file and compares them against the target Gmail account.
    /// </summary>
    public static async Task<LabelSetDiff> DiffLabelsFromFileAsync(
        IGmailApiClient client,
        string filePath,
        LabelDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        var loader = new GmailLabelLoader();
        var desiredLabels = await loader.LoadLabelsFromFileAsync(filePath);
        return await DiffLabelsAsync(client, desiredLabels, options, userId, cancellationToken);
    }
}
