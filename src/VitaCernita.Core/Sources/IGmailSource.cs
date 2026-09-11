using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Sources;

/// <summary>
/// Represents an abstract, queryable source of Gmail mailbox configurations (e.g. a live Gmail account,
/// a local Lua configuration, an in-memory test store, or a backup snapshot).
/// Exposes labels, filters, and auto-reply settings as queryable collections for LINQ composition and subset diffing.
/// </summary>
public interface IGmailSource
{
    /// <summary>
    /// Descriptive name of the source (e.g. "Gmail Account (me)", "Lua: config/gmail_filter.lua").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Retrieves a queryable collection of labels from this source.
    /// </summary>
    Task<IQueryable<GmailLabel>> GetLabelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a queryable collection of search filters from this source.
    /// </summary>
    Task<IQueryable<GmailFilter>> GetFiltersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the auto-reply (vacation responder) settings from this source.
    /// Returns null if auto-reply is not configured or disabled in this source.
    /// </summary>
    Task<AutoReply.AutoReply?> GetAutoReplyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a queryable collection of auto-reply settings from this source (0 or 1 item) for LINQ querying.
    /// </summary>
    Task<IQueryable<AutoReply.AutoReply>> GetAutoRepliesAsync(CancellationToken cancellationToken = default);
}
