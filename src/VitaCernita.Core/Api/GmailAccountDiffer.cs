using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Diffing.AutoReply;
using VitaCernita.Core.Diffing.Filters;
using VitaCernita.Core.Diffing.Labels;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
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
    public static Task<ResourceSetDiff<GmailLabel>> DiffLabelsAsync(
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
    public static Task<ResourceSetDiff<GmailLabel>> DiffLabelsFromScriptAsync(
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
    public static Task<ResourceSetDiff<GmailLabel>> DiffLabelsFromFileAsync(
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
    /// Compares desired filters against the target Gmail account.
    /// </summary>
    public static Task<ResourceSetDiff<GmailFilter>> DiffFiltersAsync(
        IGmailApiClient client,
        IEnumerable<GmailFilter> desiredFilters,
        FilterDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        var currentSource = new ApiGmailSource(client, userId);
        var desiredSource = new InMemoryGmailSource(filters: desiredFilters);
        return GmailSourceDiffer.DiffFiltersAsync(currentSource, desiredSource, options, cancellationToken);
    }

    /// <summary>
    /// Loads filters from a Lua script and compares them against the target Gmail account.
    /// </summary>
    public static Task<ResourceSetDiff<GmailFilter>> DiffFiltersFromScriptAsync(
        IGmailApiClient client,
        string luaScript,
        FilterDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        var currentSource = new ApiGmailSource(client, userId);
        var desiredSource = LuaGmailSource.FromScript(luaScript);
        return GmailSourceDiffer.DiffFiltersAsync(currentSource, desiredSource, options, cancellationToken);
    }

    /// <summary>
    /// Loads filters from a Lua file and compares them against the target Gmail account.
    /// </summary>
    public static Task<ResourceSetDiff<GmailFilter>> DiffFiltersFromFileAsync(
        IGmailApiClient client,
        string filePath,
        FilterDiffOptions? options = null,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        var currentSource = new ApiGmailSource(client, userId);
        var desiredSource = new LuaGmailSource(filePath);
        return GmailSourceDiffer.DiffFiltersAsync(currentSource, desiredSource, options, cancellationToken);
    }

    /// <summary>
    /// Compares desired auto-reply settings against the target Gmail account.
    /// </summary>
    public static Task<ResourceDiff<AutoReply.AutoReply>> DiffAutoReplyAsync(
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
    public static Task<ResourceDiff<AutoReply.AutoReply>> DiffAutoReplyFromScriptAsync(
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
    public static Task<ResourceDiff<AutoReply.AutoReply>> DiffAutoReplyFromFileAsync(
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
