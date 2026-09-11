using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Sources;

/// <summary>
/// An in-memory, mutable IGmailSource implementation for testing and pipeline composition.
/// </summary>
public class InMemoryGmailSource : IGmailSource
{
    private readonly List<GmailLabel> _labels = new();
    private readonly List<GmailFilter> _filters = new();
    private AutoReply.AutoReply? _autoReply;

    public string Name { get; set; }

    public InMemoryGmailSource(
        IEnumerable<GmailLabel>? labels = null,
        IEnumerable<GmailFilter>? filters = null,
        AutoReply.AutoReply? autoReply = null,
        string name = "InMemory")
    {
        if (labels != null) _labels.AddRange(labels);
        if (filters != null) _filters.AddRange(filters);
        _autoReply = autoReply;
        Name = name;
    }

    public InMemoryGmailSource SetAutoReply(AutoReply.AutoReply? autoReply)
    {
        _autoReply = autoReply;
        return this;
    }

    public InMemoryGmailSource AddLabel(GmailLabel label)
    {
        if (label == null) throw new ArgumentNullException(nameof(label));
        _labels.Add(label);
        return this;
    }

    public InMemoryGmailSource AddLabels(IEnumerable<GmailLabel> labels)
    {
        if (labels == null) throw new ArgumentNullException(nameof(labels));
        _labels.AddRange(labels);
        return this;
    }

    public InMemoryGmailSource AddFilter(GmailFilter filter)
    {
        if (filter == null) throw new ArgumentNullException(nameof(filter));
        _filters.Add(filter);
        return this;
    }

    public InMemoryGmailSource AddFilters(IEnumerable<GmailFilter> filters)
    {
        if (filters == null) throw new ArgumentNullException(nameof(filters));
        _filters.AddRange(filters);
        return this;
    }

    public Task<IQueryable<GmailLabel>> GetLabelsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_labels.AsQueryable());

    public Task<IQueryable<GmailFilter>> GetFiltersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_filters.AsQueryable());

    public Task<AutoReply.AutoReply?> GetAutoReplyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_autoReply);

    public Task<IQueryable<AutoReply.AutoReply>> GetAutoRepliesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_autoReply != null
            ? new[] { _autoReply }.AsQueryable()
            : Enumerable.Empty<AutoReply.AutoReply>().AsQueryable());
}
