using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Sources;

/// <summary>
/// Represents an abstract, queryable source of Gmail mailbox configurations (e.g. a live Gmail account,
/// a local Lua configuration, an in-memory test store, or a backup snapshot).
/// Exposes both labels and filters as IQueryable collections for LINQ composition and subset diffing.
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
}
