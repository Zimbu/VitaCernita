using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.AutoReply.Validation;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Labels.Validation;

namespace VitaCernita.Core.Api.Fakes;

/// <summary>
/// In-memory fake implementation of IGmailApiClient for isolated unit and integration testing.
/// Allows pre-populating labels, filters, and auto-reply, asserting call counts, and simulating HTTP/network errors.
/// </summary>
public class FakeGmailApiClient : IGmailApiClient
{
    private readonly Dictionary<string, GmailLabel> _labels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GmailFilter> _filters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AutoReply.AutoReply> _autoReplies = new(StringComparer.OrdinalIgnoreCase);

    public int ListLabelsCallCount { get; private set; }
    public int GetLabelCallCount { get; private set; }
    public int ListFiltersCallCount { get; private set; }
    public int GetFilterCallCount { get; private set; }
    public int GetAutoReplyCallCount { get; private set; }
    public int UpdateAutoReplyCallCount { get; private set; }

    public Exception? SimulatedError { get; set; }
    public HttpStatusCode? SimulatedHttpError { get; set; }

    public FakeGmailApiClient()
    {
    }

    public FakeGmailApiClient(
        IEnumerable<GmailLabel>? initialLabels,
        IEnumerable<GmailFilter>? initialFilters = null,
        AutoReply.AutoReply? initialAutoReply = null)
    {
        if (initialLabels != null) AddLabels(initialLabels);
        if (initialFilters != null) AddFilters(initialFilters);
        if (initialAutoReply != null) SetAutoReply(initialAutoReply);
    }

    public FakeGmailApiClient AddLabel(GmailLabel label)
    {
        if (label == null) throw new ArgumentNullException(nameof(label));
        string id = label.Id ?? $"Label_{Guid.NewGuid():N}";
        label.Id = id;
        _labels[id] = label;
        return this;
    }

    public FakeGmailApiClient AddLabels(IEnumerable<GmailLabel> labels)
    {
        if (labels == null) throw new ArgumentNullException(nameof(labels));
        foreach (var l in labels) AddLabel(l);
        return this;
    }

    public FakeGmailApiClient AddFilter(GmailFilter filter)
    {
        if (filter == null) throw new ArgumentNullException(nameof(filter));
        string id = filter.Id ?? $"Filter_{Guid.NewGuid():N}";
        filter.Id = id;
        _filters[id] = filter;
        return this;
    }

    public FakeGmailApiClient AddFilters(IEnumerable<GmailFilter> filters)
    {
        if (filters == null) throw new ArgumentNullException(nameof(filters));
        foreach (var f in filters) AddFilter(f);
        return this;
    }

    public FakeGmailApiClient SetAutoReply(AutoReply.AutoReply? autoReply, string userId = "me")
    {
        if (autoReply != null)
        {
            _autoReplies[userId] = autoReply;
        }
        else
        {
            _autoReplies.Remove(userId);
        }
        return this;
    }

    public void Clear()
    {
        _labels.Clear();
        _filters.Clear();
        _autoReplies.Clear();
        ResetCallCounts();
    }

    public void ResetCallCounts()
    {
        ListLabelsCallCount = 0;
        GetLabelCallCount = 0;
        ListFiltersCallCount = 0;
        GetFilterCallCount = 0;
        GetAutoReplyCallCount = 0;
        UpdateAutoReplyCallCount = 0;
    }

    public Task<IReadOnlyList<GmailLabel>> ListLabelsAsync(
        string userId = "me",
        bool onlyUserLabels = true,
        CancellationToken cancellationToken = default)
    {
        ListLabelsCallCount++;
        CheckSimulatedErrors("users.labels.list");

        var list = _labels.Values.AsEnumerable();
        if (onlyUserLabels)
        {
            list = list.Where(l => !LabelValidator.ReservedSystemLabels.Contains(l.Name));
        }

        return Task.FromResult<IReadOnlyList<GmailLabel>>(list.ToList());
    }

    public Task<GmailLabel?> GetLabelAsync(
        string labelId,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        GetLabelCallCount++;
        CheckSimulatedErrors($"users.labels.get/{labelId}");

        if (_labels.TryGetValue(labelId, out var label))
        {
            return Task.FromResult<GmailLabel?>(label);
        }

        // Also allow lookup by Name if ID not matched
        var byName = _labels.Values.FirstOrDefault(l => string.Equals(l.Name, labelId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(byName);
    }

    public Task<IReadOnlyList<GmailFilter>> ListFiltersAsync(
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        ListFiltersCallCount++;
        CheckSimulatedErrors("users.settings.filters.list");

        return Task.FromResult<IReadOnlyList<GmailFilter>>(_filters.Values.ToList());
    }

    public Task<GmailFilter?> GetFilterAsync(
        string filterId,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        GetFilterCallCount++;
        CheckSimulatedErrors($"users.settings.filters.get/{filterId}");

        _filters.TryGetValue(filterId, out var filter);
        return Task.FromResult(filter);
    }

    public Task<AutoReply.AutoReply?> GetAutoReplyAsync(
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        GetAutoReplyCallCount++;
        CheckSimulatedErrors($"users/{userId}/settings/vacation");

        if (_autoReplies.TryGetValue(userId, out var ar))
        {
            return Task.FromResult<AutoReply.AutoReply?>(ar);
        }

        // If "me" is requested and single auto-reply is set under another key, or vice-versa
        if (_autoReplies.Count == 1)
        {
            return Task.FromResult<AutoReply.AutoReply?>(_autoReplies.Values.First());
        }

        return Task.FromResult<AutoReply.AutoReply?>(null);
    }

    public Task<AutoReply.AutoReply> UpdateAutoReplyAsync(
        AutoReply.AutoReply autoReply,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (autoReply == null) throw new ArgumentNullException(nameof(autoReply));

        UpdateAutoReplyCallCount++;
        CheckSimulatedErrors($"users/{userId}/settings/vacation");

        // Simulate Google Workspace restriction: restrictToDomain is invalid for standard consumer @gmail.com accounts
        if (autoReply.RestrictToDomain && AutoReplyValidator.IsStandardGmailAccount(userId))
        {
            throw new GmailApiException(
                $"The 'restrictToDomain' setting is only available for Google Workspace users and cannot be enabled for '@gmail.com' accounts ({userId}).",
                HttpStatusCode.BadRequest,
                $"users/{userId}/settings/vacation");
        }

        _autoReplies[userId] = autoReply;
        return Task.FromResult(autoReply);
    }

    private void CheckSimulatedErrors(string endpoint)
    {
        if (SimulatedError != null)
        {
            throw SimulatedError;
        }

        if (SimulatedHttpError.HasValue)
        {
            throw new GmailApiException(
                $"Simulated HTTP error {SimulatedHttpError.Value}",
                SimulatedHttpError.Value,
                endpoint);
        }
    }
}
