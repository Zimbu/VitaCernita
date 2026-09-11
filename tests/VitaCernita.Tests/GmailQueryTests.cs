using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Queries;

namespace VitaCernita.Tests;

public class GmailQueryTests
{
    private readonly GmailFilterLoader _loader = new();
    private readonly GmailQueryLoader _queryLoader = new();

    private const string ExpectedCanonicalQuery = "from:alerts@monitoring.com subject:\"High CPU\"";
    private const string ExpectedExplicitAndQuery = "from:alerts@monitoring.com AND subject:\"High CPU\"";

    [Fact]
    public async Task Parse_DeclarativeExplicitAndMap_ProducesCorrectQuery()
    {
        string lua = @"
return {
    ['and'] = {
        from = 'alerts@monitoring.com',
        subject = 'High CPU'
    }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
        Assert.Equal(ExpectedExplicitAndQuery, rule.ToGmailQuery(explicitAnd: true));
    }

    [Fact]
    public async Task Parse_DeclarativeExplicitAndArray_ProducesCorrectQuery()
    {
        string lua = @"
return {
    ['and'] = {
        { from = 'alerts@monitoring.com' },
        { subject = 'High CPU' }
    }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_DeclarativeAllOfArray_ProducesCorrectQuery()
    {
        string lua = @"
return {
    all_of = {
        { from = 'alerts@monitoring.com' },
        { subject = 'High CPU' }
    }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_DirectTableProperties_ProducesCorrectQuery()
    {
        string lua = @"
return {
    from = 'alerts@monitoring.com',
    subject = 'High CPU'
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_FunctionalAndWithFromAndSubject_ProducesCorrectQuery()
    {
        string lua = @"
return And(
    From('alerts@monitoring.com'),
    Subject('High CPU')
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_FunctionalAndWithReversedOrder_ProducesCanonicalQuery()
    {
        // Notice: Subject is passed first, From is second.
        // Canonical ordering ensures the emitted Gmail query is identical.
        string lua = @"
return And(
    Subject('High CPU'),
    From('alerts@monitoring.com')
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_LowercaseFunctionalAllOf_ProducesCorrectQuery()
    {
        string lua = @"
return all_of(
    from('alerts@monitoring.com'),
    subject('High CPU')
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_FluentBuilder_ProducesCorrectQuery()
    {
        string lua = @"
return filter():from('alerts@monitoring.com'):subject('High CPU')
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_FluentBuilderReversedOrder_ProducesCanonicalQuery()
    {
        string lua = @"
return filter():subject('High CPU'):from('alerts@monitoring.com')
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_RuleWrapperWithMatch_ProducesCorrectQuery()
    {
        string lua = @"
return rule {
    name = 'Alerts filter',
    match = And(
        from('alerts@monitoring.com'),
        subject('High CPU')
    )
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("Alerts filter", rule.Name);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_SingleWordValues_AreNotWrappedInQuotes()
    {
        string lua = @"
return {
    ['and'] = {
        from = 'boss@example.com',
        subject = 'Urgent'
    }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:boss@example.com subject:Urgent", rule.ToGmailQuery());
    }

    // =========================================================================
    // Explicit Query DSL: query { ... }, query():...:build(), LoadQueryFromScriptAsync
    // =========================================================================

    [Fact]
    public async Task QueryDsl_DeclarativeQueryBlock_ParsesCorrectly()
    {
        string lua = @"
return query {
    from = 'alerts@monitoring.com',
    subject = 'High CPU'
}
";
        var query = await _loader.LoadQueryFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, query.ToGmailQuery());
    }

    [Fact]
    public async Task QueryDsl_FluentQueryBuilder_ProducesCanonicalQuery()
    {
        string lua = @"
return query()
    :from('alerts@monitoring.com')
    :subject('High CPU')
    :build()
";
        var query = await _loader.LoadQueryFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, query.ToGmailQuery());
    }

    [Fact]
    public async Task QueryDsl_QueryLoader_LoadsQueryDirectly()
    {
        string lua = @"
return query {
    from = 'alerts@monitoring.com',
    subject = 'High CPU'
}
";
        var query = await _queryLoader.LoadQueryFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, query.ToGmailQuery());
    }

    [Fact]
    public void GmailQuery_ClassWrapper_DelegatesToQueryCondition()
    {
        var condition = new FieldCondition("from", "alerts@monitoring.com");
        var gq = new GmailQuery(condition);

        Assert.Equal("from:alerts@monitoring.com", gq.ToGmailQuery());
        Assert.Equal("from:alerts@monitoring.com", gq.ToString());

        var gq2 = new GmailQuery(new FieldCondition("from", "alerts@monitoring.com"));
        Assert.Equal(gq, gq2);
    }
}
