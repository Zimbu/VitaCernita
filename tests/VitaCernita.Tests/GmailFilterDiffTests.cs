using System.Collections.Generic;
using System.Threading.Tasks;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Diffing.Filters;
using VitaCernita.Core.Operations.Translators;
using VitaCernita.Core.Queries;
using VitaCernita.Core.Sources;
using Xunit;

namespace VitaCernita.Tests;

public class GmailFilterDiffTests
{
    [Fact]
    public void Diff_BothNull_ReturnsUnchanged()
    {
        var diff = GmailFilterDiffer.Diff((GmailFilter?)null, (GmailFilter?)null);
        Assert.Equal(FilterDiffType.Unchanged, diff.DiffType);
        Assert.False(diff.HasChanges);
    }

    [Fact]
    public void Diff_CurrentNull_ReturnsAdded()
    {
        var desired = new GmailFilter(
            id: "filter_new",
            query: new FieldCondition("from", "newsletter@example.com"),
            action: new GmailAction().Archive().Star());

        var diff = GmailFilterDiffer.Diff(null, desired);
        Assert.Equal(FilterDiffType.Added, diff.DiffType);
        Assert.True(diff.HasChanges);
        Assert.Equal("filter_new", diff.Id);
        Assert.NotNull(diff.GetCreatePayload());
    }

    [Fact]
    public void Diff_DesiredNull_ReturnsRemoved()
    {
        var current = new GmailFilter(
            id: "filter_old",
            query: new FieldCondition("from", "spam@example.com"),
            action: new GmailAction().Delete());

        var diff = GmailFilterDiffer.Diff(current, null);
        Assert.Equal(FilterDiffType.Removed, diff.DiffType);
        Assert.True(diff.HasChanges);
        Assert.Equal("filter_old", diff.Id);
        Assert.Equal("filter_old", diff.GetDeleteId());
    }

    [Fact]
    public void Diff_IdenticalFilters_ReturnsUnchanged()
    {
        var current = new GmailFilter(
            id: "filter_1",
            query: new FieldCondition("from", "boss@company.com"),
            action: new GmailAction().MarkImportant());

        var desired = new GmailFilter(
            id: "filter_1",
            query: new FieldCondition("from", "boss@company.com"),
            action: new GmailAction().MarkImportant());

        var diff = GmailFilterDiffer.Diff(current, desired);
        Assert.Equal(FilterDiffType.Unchanged, diff.DiffType);
        Assert.False(diff.HasChanges);
        Assert.Empty(diff.FieldDifferences);
    }

    [Fact]
    public void Diff_MatchedById_AlteredAction_ReturnsModified()
    {
        // Testing user requirement: matching the filters by id allows us to know if it has been altered
        var current = new GmailFilter(
            id: "filter_100",
            query: new FieldCondition("from", "notifications@service.com"),
            action: new GmailAction().Star());

        var desired = new GmailFilter(
            id: "filter_100",
            query: new FieldCondition("from", "notifications@service.com"),
            action: new GmailAction().Star().Archive());

        var diff = GmailFilterDiffer.Diff(current, desired);
        Assert.Equal(FilterDiffType.Modified, diff.DiffType);
        Assert.True(diff.HasChanges);
        Assert.Single(diff.FieldDifferences);
        Assert.Equal("action", diff.FieldDifferences[0].FieldName);
    }

    [Fact]
    public void Diff_MatchedById_AlteredQuery_ReturnsModified()
    {
        var current = new GmailFilter(
            id: "filter_200",
            query: new FieldCondition("from", "bob@example.com"),
            action: new GmailAction().Archive());

        var desired = new GmailFilter(
            id: "filter_200",
            query: new FieldCondition("from", "alice@example.com"),
            action: new GmailAction().Archive());

        var diff = GmailFilterDiffer.Diff(current, desired);
        Assert.Equal(FilterDiffType.Modified, diff.DiffType);
        Assert.True(diff.HasChanges);
        Assert.Single(diff.FieldDifferences);
        Assert.Equal("query", diff.FieldDifferences[0].FieldName);
    }

    [Fact]
    public void Diff_BothQueryAndActionAltered_ReturnsModifiedWithMultipleDiffs()
    {
        var current = new GmailFilter(
            id: "filter_300",
            query: new FieldCondition("from", "old@example.com"),
            action: new GmailAction().Star());

        var desired = new GmailFilter(
            id: "filter_300",
            query: new FieldCondition("from", "new@example.com"),
            action: new GmailAction().Delete());

        var diff = GmailFilterDiffer.Diff(current, desired);
        Assert.Equal(FilterDiffType.Modified, diff.DiffType);
        Assert.Equal(2, diff.FieldDifferences.Count);
    }

    [Fact]
    public void DiffSets_MatchesFiltersById_AndIdentifiesAltered()
    {
        var currentFilters = new List<GmailFilter>
        {
            new("f_1", new FieldCondition("from", "a@example.com"), new GmailAction().Star()),
            new("f_2", new FieldCondition("from", "b@example.com"), new GmailAction().Archive()),
            new("f_3", new FieldCondition("from", "c@example.com"), new GmailAction().Delete())
        };

        var desiredFilters = new List<GmailFilter>
        {
            // f_1 altered action
            new("f_1", new FieldCondition("from", "a@example.com"), new GmailAction().Star().Archive()),
            // f_2 unchanged
            new("f_2", new FieldCondition("from", "b@example.com"), new GmailAction().Archive()),
            // f_new created
            new("f_new", new FieldCondition("from", "d@example.com"), new GmailAction().MarkImportant())
            // f_3 omitted -> deleted
        };

        var setDiff = GmailFilterDiffer.DiffSets(currentFilters, desiredFilters);

        Assert.True(setDiff.HasDifferences);
        Assert.Equal(1, setDiff.TotalCreations);
        Assert.Equal(1, setDiff.TotalModifications);
        Assert.Equal(1, setDiff.TotalDeletions);
        Assert.Equal(1, setDiff.TotalUnchanged);

        Assert.Equal("f_new", setDiff.Creations[0].Id);
        Assert.Equal("f_1", setDiff.Modifications[0].Id);
        Assert.Equal("f_3", setDiff.Deletions[0].Id);
        Assert.Equal("f_2", setDiff.Unchanged[0].Id);
    }

    [Fact]
    public void DiffSets_MatchByQuery_MatchesWhenIdsAreDifferent()
    {
        var currentFilters = new List<GmailFilter>
        {
            new("id_server_99", new FieldCondition("from", "vendor@example.com"), new GmailAction().Star())
        };

        var desiredFilters = new List<GmailFilter>
        {
            // No ID in desired configuration, matches by query
            new(null, new FieldCondition("from", "vendor@example.com"), new GmailAction().Star().Archive())
        };

        var setDiff = GmailFilterDiffer.DiffSets(
            currentFilters,
            desiredFilters,
            new FilterDiffOptions { MatchBy = FilterMatchKey.Query });

        Assert.Equal(1, setDiff.TotalModifications);
        Assert.Equal("id_server_99", setDiff.Modifications[0].Id);
    }

    [Fact]
    public void DiffSets_MatchByIdStrict_TreatsDifferentIdsAsAddAndDelete()
    {
        var currentFilters = new List<GmailFilter>
        {
            new("id_server_1", new FieldCondition("from", "test@example.com"), new GmailAction().Star())
        };

        var desiredFilters = new List<GmailFilter>
        {
            new("id_local_2", new FieldCondition("from", "test@example.com"), new GmailAction().Star())
        };

        var setDiff = GmailFilterDiffer.DiffSets(
            currentFilters,
            desiredFilters,
            new FilterDiffOptions { MatchBy = FilterMatchKey.Id });

        Assert.Equal(1, setDiff.TotalCreations);
        Assert.Equal(1, setDiff.TotalDeletions);
        Assert.Equal(0, setDiff.TotalModifications);
    }

    [Fact]
    public void DiffSets_FieldsToIgnore_IgnoresQuery()
    {
        var current = new GmailFilter("f1", new FieldCondition("from", "a@x.com"), new GmailAction().Star());
        var desired = new GmailFilter("f1", new FieldCondition("from", "b@x.com"), new GmailAction().Star());

        var setDiff = GmailFilterDiffer.DiffSets(
            new[] { current },
            new[] { desired },
            new FilterDiffOptions { FieldsToIgnore = new HashSet<string> { "query" } });

        Assert.False(setDiff.HasDifferences);
        Assert.Equal(1, setDiff.TotalUnchanged);
    }

    [Fact]
    public void DiffSets_FieldsToCompare_OnlyComparesAction()
    {
        var current = new GmailFilter("f1", new FieldCondition("from", "a@x.com"), new GmailAction().Star());
        var desired = new GmailFilter("f1", new FieldCondition("from", "b@x.com"), new GmailAction().Archive());

        var setDiff = GmailFilterDiffer.DiffSets(
            new[] { current },
            new[] { desired },
            new FilterDiffOptions { FieldsToCompare = new HashSet<string> { "action" } });

        Assert.True(setDiff.HasDifferences);
        Assert.Equal(1, setDiff.TotalModifications);
        Assert.Single(setDiff.Modifications[0].FieldDifferences);
        Assert.Equal("action", setDiff.Modifications[0].FieldDifferences[0].FieldName);
    }

    [Fact]
    public void DiffApiListResponse_ParsesAndDiffsCorrectly()
    {
        string apiJson = """
        {
          "filter": [
            {
              "id": "Filter_API_1",
              "criteria": { "query": "from:alice" },
              "action": { "addLabelIds": ["STARRED"] }
            }
          ]
        }
        """;

        var desired = new List<GmailFilter>
        {
            new("Filter_API_1", new RawQueryCondition("from:alice"), new GmailAction().Star().Archive())
        };

        var setDiff = GmailFilterDiffer.DiffApiListResponse(apiJson, desired);
        Assert.Equal(1, setDiff.TotalModifications);
        Assert.Equal("Filter_API_1", setDiff.Modifications[0].Id);
    }

    [Fact]
    public void ToDryRunReport_OutputsFormattedSummary()
    {
        var currentFilters = new List<GmailFilter>
        {
            new("f_1", new FieldCondition("from", "a@example.com"), new GmailAction().Star()),
            new("f_del", new FieldCondition("from", "old@example.com"), new GmailAction().Archive())
        };

        var desiredFilters = new List<GmailFilter>
        {
            new("f_1", new FieldCondition("from", "a@example.com"), new GmailAction().Star().Archive()),
            new("f_new", new FieldCondition("from", "new@example.com"), new GmailAction().Delete())
        };

        var setDiff = GmailFilterDiffer.DiffSets(currentFilters, desiredFilters);
        string report = setDiff.ToDryRunReport();

        Assert.Contains("VitaCernita Filter Diff Report (Dry Run)", report);
        Assert.Contains("[+] Create (1)", report);
        Assert.Contains("[~] Update (1)", report);
        Assert.Contains("[-] Delete (1)", report);
        Assert.Contains("f_new", report);
        Assert.Contains("f_1", report);
        Assert.Contains("f_del", report);
    }

    [Fact]
    public async Task GmailSourceDiffer_DiffFiltersAsync_WorksAcrossSources()
    {
        var currentSource = new InMemoryGmailSource(filters: new[]
        {
            new GmailFilter("f_existing", new FieldCondition("from", "test@example.com"), new GmailAction().Star())
        });

        var desiredSource = new InMemoryGmailSource(filters: new[]
        {
            new GmailFilter("f_existing", new FieldCondition("from", "test@example.com"), new GmailAction().Star().Archive())
        });

        var diff = await GmailSourceDiffer.DiffFiltersAsync(currentSource, desiredSource);
        Assert.Equal(1, diff.TotalModifications);
        Assert.Equal("f_existing", diff.Modifications[0].Id);
    }
}
