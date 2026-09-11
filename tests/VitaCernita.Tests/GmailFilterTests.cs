using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Queries;

namespace VitaCernita.Tests;

/// <summary>
/// Unit tests for GmailFilter: combination of Id, Query (criteria), and Action.
/// Verifies proper scoping and decoupling of Filter vs Query vs Action matching Google Gmail API users.settings.filters.
/// </summary>
public class GmailFilterTests
{
    private readonly GmailFilterLoader _loader = new();

    // =========================================================================
    // 1. Full Filter: Id + Query + Action
    // =========================================================================

    [Fact]
    public async Task Filter_WithIdQueryAndAction_DeclarativeDsl_ParsesCorrectly()
    {
        string lua = @"
return filter {
    id = 'filter-sec-01',
    name = 'Security Incident Triage',
    query = query {
        from = 'secops@company.com',
        subject = 'CRITICAL'
    },
    action = actions(archive, star, mark_important, add_category('Updates'), add_label('Security'))
}
";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.Equal("filter-sec-01", filter.Id);
        Assert.Equal("Security Incident Triage", filter.Name);
        Assert.NotNull(filter.Query);
        Assert.NotNull(filter.Action);
        Assert.Equal("from:secops@company.com subject:CRITICAL", filter.ToGmailQuery());

        Assert.True(filter.Action.IsArchive);
        Assert.True(filter.Action.IsStarred);
        Assert.True(filter.Action.IsImportant);
        Assert.Equal(SystemLabels.CategoryUpdates, filter.Action.Category);
        Assert.Contains("Security", filter.Action.CustomLabels);
    }

    [Fact]
    public async Task Filter_WithIdQueryAndAction_FluentDsl_ParsesCorrectly()
    {
        string lua = @"
return filter()
    :id('filter-fin-99')
    :name('Billing Automation')
    :query(query():from('billing@stripe.com'):subject('Invoice'):build())
    :actions(archive, add_category('Purchases'), add_label('Stripe'))
    :build()
";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.Equal("filter-fin-99", filter.Id);
        Assert.Equal("Billing Automation", filter.Name);
        Assert.NotNull(filter.Query);
        Assert.NotNull(filter.Action);
        Assert.Equal("from:billing@stripe.com subject:Invoice", filter.ToGmailQuery());
        Assert.True(filter.Action.IsArchive);
        Assert.Equal(SystemLabels.CategoryPurchases, filter.Action.Category);
        Assert.Contains("Stripe", filter.Action.CustomLabels);
    }

    [Fact]
    public async Task Filter_FluentChainedQueryAndAction_ParsesCorrectly()
    {
        string lua = @"
return filter()
    :id('filter-chain-01')
    :name('Chained Filter')
    :from('boss@example.com')
    :subject('Urgent Meeting')
    :action(star)
    :build()
";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.Equal("filter-chain-01", filter.Id);
        Assert.Equal("Chained Filter", filter.Name);
        Assert.NotNull(filter.Query);
        Assert.NotNull(filter.Action);
        Assert.Equal("from:boss@example.com subject:\"Urgent Meeting\"", filter.ToGmailQuery());
        Assert.True(filter.Action.IsStarred);
    }

    // =========================================================================
    // 2. Partial Filters (Decoupled Combinations)
    // =========================================================================

    [Fact]
    public async Task Filter_WithIdAndQueryOnly_HasNoAction()
    {
        string lua = @"
return filter {
    id = 'filter-query-only',
    name = 'Alert Search',
    query = query { from = 'alerts@monitoring.com' }
}
";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.Equal("filter-query-only", filter.Id);
        Assert.Equal("Alert Search", filter.Name);
        Assert.NotNull(filter.Query);
        Assert.Null(filter.Action);
        Assert.Equal("from:alerts@monitoring.com", filter.ToGmailQuery());
    }

    [Fact]
    public async Task Filter_WithQueryAndActionOnly_HasNoId()
    {
        string lua = @"
return filter {
    query = query { from = 'newsletter@news.com' },
    action = actions(archive, mark_read)
}
";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.Null(filter.Id);
        Assert.NotNull(filter.Query);
        Assert.NotNull(filter.Action);
        Assert.Equal("from:newsletter@news.com", filter.ToGmailQuery());
        Assert.True(filter.Action.IsArchive);
        Assert.True(filter.Action.IsMarkUnread);
    }

    [Fact]
    public async Task Filter_WithIdAndActionOnly_HasNoQuery()
    {
        string lua = @"
return filter {
    id = 'filter-action-only',
    action = actions(delete)
}
";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.Equal("filter-action-only", filter.Id);
        Assert.Null(filter.Query);
        Assert.NotNull(filter.Action);
        Assert.Equal(string.Empty, filter.ToGmailQuery());
        Assert.True(filter.Action.IsDelete);
    }

    [Fact]
    public async Task Filter_WithIdOnly_ConstructsValidFilter()
    {
        string lua = @"
return filter {
    id = 'filter-id-only'
}
";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.Equal("filter-id-only", filter.Id);
        Assert.Null(filter.Query);
        Assert.Null(filter.Action);
    }

    // =========================================================================
    // 3. ToDictionary() Serialization (Gmail API users.settings.filters Schema)
    // =========================================================================

    [Fact]
    public void ToDictionary_FullFilter_ProducesExpectedGmailApiStructure()
    {
        var query = new AndCondition(new IQueryCondition[]
        {
            new FieldCondition("from", "boss@example.com"),
            new FieldCondition("subject", "Urgent")
        });

        var action = new GmailAction()
            .Archive()
            .Star()
            .AddCategory("Purchases")
            .SetForward("assistant@example.com");

        var filter = new GmailFilter(id: "filter-api-01", query: query, action: action, name: "Boss Priority");

        var dict = filter.ToDictionary();

        Assert.Equal("filter-api-01", dict["id"]);

        Assert.True(dict.ContainsKey("criteria"));
        var criteria = Assert.IsType<Dictionary<string, object>>(dict["criteria"]);
        Assert.Equal("from:boss@example.com subject:Urgent", criteria["query"]);

        Assert.True(dict.ContainsKey("action"));
        var actionDict = Assert.IsType<Dictionary<string, object>>(dict["action"]);
        Assert.Equal("assistant@example.com", actionDict["forward"]);

        var addLabelIds = Assert.IsType<List<string>>(actionDict["addLabelIds"]);
        Assert.Contains(SystemLabels.Starred, addLabelIds);
        Assert.Contains(SystemLabels.CategoryPurchases, addLabelIds);

        var removeLabelIds = Assert.IsType<List<string>>(actionDict["removeLabelIds"]);
        Assert.Contains(SystemLabels.Inbox, removeLabelIds);
    }

    [Fact]
    public void ToDictionary_FilterWithoutId_OmitsIdField()
    {
        var query = new FieldCondition("from", "test@example.com");
        var action = new GmailAction().Star();
        var filter = new GmailFilter(query: query, action: action);

        var dict = filter.ToDictionary();

        Assert.False(dict.ContainsKey("id"));
        Assert.True(dict.ContainsKey("criteria"));
        Assert.True(dict.ContainsKey("action"));
    }

    [Fact]
    public void ToDictionary_FilterWithoutQuery_OmitsCriteriaField()
    {
        var action = new GmailAction().Delete();
        var filter = new GmailFilter(id: "filter-no-query", action: action);

        var dict = filter.ToDictionary();

        Assert.Equal("filter-no-query", dict["id"]);
        Assert.False(dict.ContainsKey("criteria"));
        Assert.True(dict.ContainsKey("action"));
    }

    [Fact]
    public void ToDictionary_FilterWithoutAction_OmitsActionField()
    {
        var query = new FieldCondition("from", "test@example.com");
        var filter = new GmailFilter(id: "filter-no-action", query: query);

        var dict = filter.ToDictionary();

        Assert.Equal("filter-no-action", dict["id"]);
        Assert.True(dict.ContainsKey("criteria"));
        Assert.False(dict.ContainsKey("action"));
    }

    // =========================================================================
    // 4. Backward Compatibility (Criteria, Condition, GmailRule)
    // =========================================================================

    [Fact]
    public void BackwardCompatibility_CriteriaAndConditionProperties_MapToQuery()
    {
        var cond = new FieldCondition("to", "team@example.com");
        var filter = new GmailFilter(criteria: cond);

        Assert.Same(cond, filter.Query);
        Assert.Same(cond, filter.Criteria);
        Assert.Same(cond, filter.Condition);

        var newCond = new FieldCondition("to", "other@example.com");
        filter.Criteria = newCond;
        Assert.Same(newCond, filter.Query);

        var thirdCond = new FieldCondition("to", "third@example.com");
        filter.Condition = thirdCond;
        Assert.Same(thirdCond, filter.Query);
    }

    [Fact]
    public async Task BackwardCompatibility_RuleFunction_LoadsAsGmailRule()
    {
        string lua = @"
return rule {
    id = 'rule-01',
    name = 'Legacy Rule',
    match = And(From('a@b.com'), Subject('Hello')),
    action = actions(archive)
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        Assert.IsType<GmailRule>(rule);
        Assert.Equal("rule-01", rule.Id);
        Assert.Equal("Legacy Rule", rule.Name);
        Assert.Equal("from:a@b.com subject:Hello", rule.ToGmailQuery());
        Assert.NotNull(rule.Action);
        Assert.True(rule.Action.IsArchive);
    }

    // =========================================================================
    // 5. Filter Equality & Hash Code
    // =========================================================================

    [Fact]
    public void Filter_Equality_SameValuesAreEqual()
    {
        var q1 = new FieldCondition("from", "alerts@test.com");
        var q2 = new FieldCondition("from", "alerts@test.com");
        var a1 = new GmailAction().Archive();
        var a2 = new GmailAction().Archive();

        var f1 = new GmailFilter(id: "id-1", query: q1, action: a1, name: "Name");
        var f2 = new GmailFilter(id: "id-1", query: q2, action: a2, name: "Name");

        Assert.Equal(f1, f2);
        Assert.Equal(f1.GetHashCode(), f2.GetHashCode());
    }

    [Fact]
    public void Filter_Equality_DifferentPropertiesAreNotEqual()
    {
        var q = new FieldCondition("from", "alerts@test.com");
        var a = new GmailAction().Archive();

        var f1 = new GmailFilter(id: "id-1", query: q, action: a);
        var f2 = new GmailFilter(id: "id-2", query: q, action: a);
        var f3 = new GmailFilter(id: "id-1", query: q, action: new GmailAction().Star());

        Assert.NotEqual(f1, f2);
        Assert.NotEqual(f1, f3);
    }

    // =========================================================================
    // 6. Loading Multiple Filters
    // =========================================================================

    [Fact]
    public async Task LoadFilters_MultipleFiltersTable_LoadsAllFilters()
    {
        string lua = @"
return {
    filters = {
        filter {
            id = 'filter-01',
            query = query { from = 'a@test.com' },
            action = actions(archive)
        },
        filter {
            id = 'filter-02',
            query = query { from = 'b@test.com' },
            action = actions(star)
        }
    }
}
";
        var filters = await _loader.LoadFiltersFromScriptAsync(lua);

        Assert.Equal(2, filters.Count);
        Assert.Equal("filter-01", filters[0].Id);
        Assert.True(filters[0].Action?.IsArchive);
        Assert.Equal("filter-02", filters[1].Id);
        Assert.True(filters[1].Action?.IsStarred);
    }
}
