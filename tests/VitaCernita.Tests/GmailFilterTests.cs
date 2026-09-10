using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Filters;

namespace VitaCernita.Tests;

public class GmailFilterTests
{
    private readonly GmailFilterLoader _loader = new();

    private const string ExpectedCanonicalQuery = "from:alerts@monitoring.com subject:\"High CPU\"";
    private const string ExpectedExplicitAndQuery = "from:alerts@monitoring.com AND subject:\"High CPU\"";

    [Fact]
    public async Task Parse_DeclarativeExplicitAndMap_ProducesCorrectFilter()
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
    public async Task Parse_DeclarativeExplicitAndArray_ProducesCorrectFilter()
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
    public async Task Parse_DeclarativeAllOfArray_ProducesCorrectFilter()
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
    public async Task Parse_DirectTableProperties_ProducesCorrectFilter()
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
    public async Task Parse_FunctionalAndWithFromAndSubject_ProducesCorrectFilter()
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
    public async Task Parse_FunctionalAndWithReversedOrder_ProducesCanonicalFilter()
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
    public async Task Parse_LowercaseFunctionalAllOf_ProducesCorrectFilter()
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
    public async Task Parse_FluentBuilder_ProducesCorrectFilter()
    {
        string lua = @"
return filter():from('alerts@monitoring.com'):subject('High CPU')
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_FluentBuilderReversedOrder_ProducesCanonicalFilter()
    {
        string lua = @"
return filter():subject('High CPU'):from('alerts@monitoring.com')
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(ExpectedCanonicalQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_RuleWrapperWithMatch_ProducesCorrectFilter()
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
}
