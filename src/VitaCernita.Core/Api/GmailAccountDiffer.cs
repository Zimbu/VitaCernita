using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.AutoReply.Diff;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Labels.Diff;
using VitaCernita.Core.Sources;

namespace VitaCernita.Core.Api;

/// <summary>
/// Convenience adapter for diffing local Lua configurations against a target Gmail account via IGmailApiClient.
/// Backed by the agnostic GmailSourceDiffer.
/// </summary>
public static class GmailAccountDiffer
{
    /// <summary>
    /// Compares desired labels against the target Gmail account.
    /// </summary>
    public static Task<LabelSetDiff> DiffLabelsAsync(
        IGmailApiClient client,
        IEnumerable<GmailLabel> desiredLabels,
        LabelDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        var currentSource = new ApiGmailSource(client, userId);
        var desiredSource = new InMemoryGmailSource(desiredLabels);
        return GmailSourceDiffer.DiffLabelsAsync(currentSource, desiredSource, options, cancellationToken);
    }

    /// <summary>
    /// Loads labels from a Lua script and compares them against the target Gmail account.
    /// </summary>
    public static Task<LabelSetDiff> DiffLabelsFromScriptAsync(
        IGmailApiClient client,
        string luaScript,
        LabelDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        var currentSource = new ApiGmailSource(client, userId);
        var desiredSource = LuaGmailSource.FromScript(luaScript);
        return GmailSourceDiffer.DiffLabelsAsync(currentSource, desiredSource, options, cancellationToken);
    }

    /// <summary>
    /// Loads labels from a Lua file and compares them against the target Gmail account.
    /// </summary>
    public static Task<LabelSetDiff> DiffLabelsFromFileAsync(
        IGmailApiClient client,
        string filePath,
        LabelDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        var currentSource = new ApiGmailSource(client, userId);
        var desiredSource = new LuaGmailSource(filePath);
        return GmailSourceDiffer.DiffLabelsAsync(currentSource, desiredSource, options, cancellationToken);
    }

    /// <summary>
    /// Compares desired auto-reply settings against the target Gmail account.
    /// </summary>
    public static Task<AutoReplyDiff> DiffAutoReplyAsync(
        IGmailApiClient client,
        AutoReply.AutoReply desiredAutoReply,
        AutoReplyDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        options ??= new AutoReplyDiffOptions();
        options.TargetAccount ??= userId;

        var currentSource = new ApiGmailSource(client, userId);
        var desiredSource = new InMemoryGmailSource(autoReply: desiredAutoReply);
        return GmailSourceDiffer.DiffAutoReplyAsync(currentSource, desiredSource, options, cancellationToken);
    }

    /// <summary>
    /// Loads auto-reply settings from a Lua script and compares them against the target Gmail account.
    /// </summary>
    public static Task<AutoReplyDiff> DiffAutoReplyFromScriptAsync(
        IGmailApiClient client,
        string luaScript,
        AutoReplyDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        options ??= new AutoReplyDiffOptions();
        options.TargetAccount ??= userId;

        var currentSource = new ApiGmailSource(client, userId);
        var desiredSource = LuaGmailSource.FromScript(luaScript);
        return GmailSourceDiffer.DiffAutoReplyAsync(currentSource, desiredSource, options, cancellationToken);
    }

    /// <summary>
    /// Loads auto-reply settings from a Lua file and compares them against the target Gmail account.
    /// </summary>
    public static Task<AutoReplyDiff> DiffAutoReplyFromFileAsync(
        IGmailApiClient client,
        string filePath,
        AutoReplyDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        options ??= new AutoReplyDiffOptions();
        options.TargetAccount ??= userId;

        var currentSource = new ApiGmailSource(client, userId);
        var desiredSource = new LuaGmailSource(filePath);
        return GmailSourceDiffer.DiffAutoReplyAsync(currentSource, desiredSource, options, cancellationToken);
    }
}
