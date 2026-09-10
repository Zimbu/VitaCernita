using System.Threading.Tasks;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Filters.Validation;
using Xunit;

namespace VitaCernita.Tests;

public class NotOperatorTests
{
    private readonly GmailFilterLoader _loader = new();

    // =========================================================================
    // 1. Negation of Single Fields & Operators
    // =========================================================================

    [Fact]
    public async Task Not_TableWithField_NegatesIsStarred()
    {
        // User explicit requirement: not({ is_starred = true }) -> -is:starred
        string lua = "return not({ is_starred = true })";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-is:starred", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Node_NegatesIsStarred()
    {
        string lua = "return not(is_starred)";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-is:starred", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Node_NegatesFieldCondition()
    {
        string lua = "return not(From('spammer@evil.com'))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-from:spammer@evil.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Node_NegatesStarAndIcon()
    {
        string lua = "return not(has_yellow_star)";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-has:yellow-star", rule.ToGmailQuery());

        string luaBang = "return not(has_red_bang)";
        var ruleBang = await _loader.LoadRuleFromScriptAsync(luaBang);
        Assert.Equal("-has:red-bang", ruleBang.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Node_NegatesExactPhrase()
    {
        string lua = "return not(match('confidential audit'))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-\"confidential audit\"", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Node_NegatesDateAndDuration()
    {
        string luaDate = "return not(after('2026/01/01'))";
        var ruleDate = await _loader.LoadRuleFromScriptAsync(luaDate);
        Assert.Equal("-after:2026/01/01", ruleDate.ToGmailQuery());

        string luaDur = "return not(older_than('90d'))";
        var ruleDur = await _loader.LoadRuleFromScriptAsync(luaDur);
        Assert.Equal("-older_than:90d", ruleDur.ToGmailQuery());
    }

    // =========================================================================
    // 2. Boundary Value Analysis: Negating And & Or Expressions
    // =========================================================================

    [Fact]
    public async Task Not_And_SingleConditionBoundary_EmitsSingleTermWithoutParens()
    {
        // 1-condition boundary in AND: not(And(From("a"))) -> -from:a
        string lua = "return not(And(From('corp@example.com')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-from:corp@example.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_And_TwoConditionsBoundary_EmitsParenthesizedAnd()
    {
        // 2-conditions boundary in AND
        string lua = "return not(And(From('alice@example.com'), Subject('Audit')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com subject:Audit)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_And_ThreeConditionsBoundary_EmitsParenthesizedAnd()
    {
        // 3-conditions boundary in AND
        string lua = "return not(And(From('alice@example.com'), To('bob@example.com'), Subject('Audit')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com to:bob@example.com subject:Audit)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_And_ExplicitAndKeyword_EmitsParenthesizedExplicitAnd()
    {
        string lua = "return not(And(From('a@example.com'), To('b@example.com')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:a@example.com AND to:b@example.com)", rule.ToGmailQuery(explicitAnd: true));
    }

    [Fact]
    public async Task Not_Or_SingleConditionBoundary_EmitsSingleTermWithoutParens()
    {
        // 1-condition boundary in OR: not(Or(From("a"))) -> -from:a
        string lua = "return not(Or(From('corp@example.com')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-from:corp@example.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Or_TwoConditionsBoundary_EmitsParenthesizedOr()
    {
        // 2-conditions boundary in OR
        string lua = "return not(Or(From('a@example.com'), From('b@example.com')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:a@example.com OR from:b@example.com)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Or_ThreeConditionsBoundary_EmitsParenthesizedOr()
    {
        // 3-conditions boundary in OR
        string lua = "return not(Or(From('a@example.com'), From('b@example.com'), From('c@example.com')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:a@example.com OR from:b@example.com OR from:c@example.com)", rule.ToGmailQuery());
    }

    // =========================================================================
    // 3. Nesting: And containing Not, Or containing Not, Nested Not
    // =========================================================================

    [Fact]
    public async Task And_ContainingNot_EmitsCorrectQuery()
    {
        string lua = @"
return And(
    From('boss@company.com'),
    not(Subject('Weekly Status'))
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:boss@company.com -subject:\"Weekly Status\"", rule.ToGmailQuery());
    }

    [Fact]
    public async Task And_ContainingNotOr_EmitsCorrectQuery()
    {
        string lua = @"
return And(
    From('boss@company.com'),
    not(Or(Subject('Out of Office'), Subject('Vacation')))
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:boss@company.com -(subject:\"Out of Office\" OR subject:Vacation)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Or_ContainingNot_EmitsCorrectQuery()
    {
        string lua = @"
return Or(
    From('boss@company.com'),
    not(is_starred)
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        // Sorted alphabetically: "-is:starred OR from:boss@company.com"
        Assert.Equal("-is:starred OR from:boss@company.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Or_ContainingNotAnd_EmitsCorrectQuery()
    {
        string lua = @"
return Or(
    From('boss@company.com'),
    not(And(To('archive@company.com'), Subject('Logs')))
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(to:archive@company.com subject:Logs) OR from:boss@company.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task DoubleNegation_EmitsParenthesizedNegation()
    {
        string lua = "return not(not(From('trusted@company.com')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(-from:trusted@company.com)", rule.ToGmailQuery());
    }

    // =========================================================================
    // 4. Table Syntax & FilterBuilder
    // =========================================================================

    [Fact]
    public async Task TableSyntax_NotKey_EmitsCorrectQuery()
    {
        string lua = @"
return {
    from = 'finance@company.com',
    ['not'] = { is_starred = true }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:finance@company.com -is:starred", rule.ToGmailQuery());
    }

    [Fact]
    public async Task FilterBuilder_NotMethod_EmitsCorrectQuery()
    {
        string lua = @"
return filter():from('compliance@company.com'):not(Or(Filename('test.exe'), Filename('malware.bat'))):build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:compliance@company.com -(filename:malware.bat OR filename:test.exe)", rule.ToGmailQuery());
    }

    // =========================================================================
    // 5. Negative & Empty Cases (Must Throw FilterValidationException)
    // =========================================================================

    [Fact]
    public async Task Not_EmptyCall_ThrowsValidationException()
    {
        string lua = "return not()";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Fact]
    public async Task Not_EmptyTable_ThrowsValidationException()
    {
        string lua = "return not({})";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Fact]
    public async Task Not_EmptyString_ThrowsValidationException()
    {
        string luaEmpty = "return not('')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(luaEmpty));

        string luaWhitespace = "return not('   ')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(luaWhitespace));
    }

    [Fact]
    public async Task TableSyntax_EmptyNotKey_ThrowsValidationException()
    {
        string lua = @"
return {
    from = 'corp@company.com',
    ['not'] = {}
}
";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Fact]
    public async Task FilterBuilder_EmptyNot_ThrowsValidationException()
    {
        string lua = @"
return filter():from('corp@company.com'):Not():build()
";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }
}
