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
    // 2. Boundary Value Analysis: Negating And & Or Operators (1-ary, 2-ary, 3-ary, N-ary)
    //    Verifies that 'and' and 'or' are tested as binary-to-n-ary operators
    //    connecting multiple distinct fields under 'not()', ensuring the operator
    //    and distinct fields are never ignored or collapsed into a unary construct.
    // =========================================================================

    [Fact]
    public async Task Not_And_SingleConditionBoundary_EmitsSingleTermWithoutParens()
    {
        // 1-condition boundary in AND: not(And(From("a"))) -> -from:a (unary boundary)
        string lua = "return not(And(From('corp@example.com')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-from:corp@example.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_And_TwoDistinctFields_BinaryBoundary_EmitsParenthesizedAnd()
    {
        // 2-conditions binary boundary in AND with multiple distinct fields
        string lua = "return not(And(From('alice@example.com'), Subject('Audit')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com subject:Audit)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_And_ThreeDistinctFields_TernaryBoundary_EmitsParenthesizedAnd()
    {
        // 3-conditions ternary boundary in AND with multiple distinct fields
        string lua = "return not(And(From('alice@example.com'), To('bob@example.com'), Subject('Audit')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com to:bob@example.com subject:Audit)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_And_FourDistinctFields_QuaternaryBoundary_EmitsParenthesizedAnd()
    {
        // 4-conditions quaternary boundary in AND with multiple distinct fields
        string lua = "return not(And(From('alice@example.com'), To('bob@example.com'), Subject('Audit'), Label('compliance')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com to:bob@example.com subject:Audit label:compliance)", rule.ToGmailQuery());

        string expectedExplicit = "-(from:alice@example.com AND to:bob@example.com AND subject:Audit AND label:compliance)";
        Assert.Equal(expectedExplicit, rule.ToGmailQuery(explicitAnd: true));
    }

    [Fact]
    public async Task Not_And_FiveDistinctFields_NaryBoundary_EmitsParenthesizedAnd()
    {
        // 5-conditions n-ary boundary in AND with multiple distinct fields
        string lua = "return not(And(From('alice@example.com'), To('bob@example.com'), Subject('Audit'), Filename('report.pdf'), Label('compliance')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com to:bob@example.com subject:Audit filename:report.pdf label:compliance)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_And_DiverseFieldTypes_EmitsParenthesizedAnd()
    {
        // AND containing diverse operator types: from, subject, has (star), is, older_than
        string lua = "return not(And(From('alice@example.com'), Subject('Audit'), has_green_check, is_starred, older_than('30d')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com subject:Audit has:green-check is:starred older_than:30d)", rule.ToGmailQuery());
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
        // 1-condition boundary in OR: not(Or(From("a"))) -> -from:a (unary boundary)
        string lua = "return not(Or(From('corp@example.com')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-from:corp@example.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Or_TwoConditionsSameField_EmitsParenthesizedOr()
    {
        // 2-conditions boundary in OR with same field
        string lua = "return not(Or(From('a@example.com'), From('b@example.com')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:a@example.com OR from:b@example.com)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Or_TwoDistinctFields_BinaryBoundary_EmitsParenthesizedOr()
    {
        // 2-conditions binary boundary in OR with multiple distinct fields (from, subject)
        string lua = "return not(Or(From('alice@example.com'), Subject('Critical Alert')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com OR subject:\"Critical Alert\")", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Or_ThreeConditionsSameField_EmitsParenthesizedOr()
    {
        // 3-conditions boundary in OR with same field
        string lua = "return not(Or(From('a@example.com'), From('b@example.com'), From('c@example.com')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:a@example.com OR from:b@example.com OR from:c@example.com)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Or_ThreeDistinctFields_TernaryBoundary_EmitsParenthesizedOr()
    {
        // 3-conditions ternary boundary in OR with multiple distinct fields (from, subject, label)
        string lua = "return not(Or(From('alice@example.com'), Subject('Critical Alert'), Label('urgent')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com OR label:urgent OR subject:\"Critical Alert\")", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Or_FourDistinctFields_QuaternaryBoundary_EmitsParenthesizedOr()
    {
        // 4-conditions quaternary boundary in OR with multiple distinct fields (from, to, subject, label)
        string lua = "return not(Or(From('alice@example.com'), To('bob@example.com'), Subject('Critical Alert'), Label('urgent')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com OR label:urgent OR subject:\"Critical Alert\" OR to:bob@example.com)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Or_FiveDistinctFields_NaryBoundary_EmitsParenthesizedOr()
    {
        // 5-conditions n-ary boundary in OR with multiple distinct fields (from, to, subject, label, filename)
        string lua = "return not(Or(From('alice@example.com'), To('bob@example.com'), Subject('Critical Alert'), Label('urgent'), Filename('payload.exe')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(filename:payload.exe OR from:alice@example.com OR label:urgent OR subject:\"Critical Alert\" OR to:bob@example.com)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_Or_DiverseFieldTypes_EmitsParenthesizedOr()
    {
        // OR containing diverse operator types: from, is, has, older_than
        string lua = "return not(Or(From('alice@example.com'), is_starred, has_yellow_star, older_than('30d')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com OR has:yellow-star OR is:starred OR older_than:30d)", rule.ToGmailQuery());
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
    public async Task Not_Or_SurroundingAndsWithMultipleDistinctFields_EmitsNestedQuery()
    {
        string lua = @"
return not(Or(
    And(From('secops@company.com'), Subject('Security Breach'), Label('sev-1')),
    And(From('devops@company.com'), Subject('Cluster Outage'), Label('sev-1'))
))
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-((from:devops@company.com subject:\"Cluster Outage\" label:sev-1) OR (from:secops@company.com subject:\"Security Breach\" label:sev-1))", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_And_SurroundingOrWithMultipleDistinctFields_EmitsNestedQuery()
    {
        string lua = @"
return not(And(
    From('exec@company.com'),
    To('all@company.com'),
    Subject('Update'),
    Or(Label('confidential'), Filename('secret.pdf'))
))
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:exec@company.com to:all@company.com subject:Update (filename:secret.pdf OR label:confidential))", rule.ToGmailQuery());
    }

    [Fact]
    public async Task DoubleNegation_EmitsParenthesizedNegation()
    {
        string lua = "return not(not(From('trusted@company.com')))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(-from:trusted@company.com)", rule.ToGmailQuery());
    }

    // =========================================================================
    // 4. Table Syntax & FilterBuilder (with Multiple Fields)
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
    public async Task TableSyntax_NotWithMultipleFieldsImplicitAnd_EmitsParenthesizedAnd()
    {
        string lua = @"
return {
    ['not'] = {
        from = 'alice@example.com',
        to = 'bob@example.com',
        subject = 'Confidential',
        label = 'finance'
    }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com to:bob@example.com subject:Confidential label:finance)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_NotWithExplicitOrMultipleDistinctFields_EmitsParenthesizedOr()
    {
        string lua = @"
return {
    ['not'] = {
        ['or'] = {
            { from = 'alice@example.com' },
            { subject = 'Phishing Alert' },
            { label = 'quarantine' },
            { to = 'security@example.com' }
        }
    }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com OR label:quarantine OR subject:\"Phishing Alert\" OR to:security@example.com)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_NotWithExplicitAndMultipleDistinctFields_EmitsParenthesizedAnd()
    {
        string lua = @"
return {
    ['not'] = {
        ['and'] = {
            { from = 'alice@example.com' },
            { to = 'bob@example.com' },
            { subject = 'Wire Transfer' },
            { label = 'finance' }
        }
    }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com to:bob@example.com subject:\"Wire Transfer\" label:finance)", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Not_MultiArgumentConvenience_EmitsParenthesizedAnd()
    {
        string lua = "return not(From('alice@example.com'), To('bob@example.com'), Subject('Audit'))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-(from:alice@example.com to:bob@example.com subject:Audit)", rule.ToGmailQuery());
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

    [Fact]
    public async Task FilterBuilder_NotMethod_WithMultipleDistinctFields_EmitsCorrectQuery()
    {
        string lua = @"
return filter():from('compliance@company.com'):not(Or(Subject('Confidential'), Label('audit'), To('external@evil.com'))):build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:compliance@company.com -(label:audit OR subject:Confidential OR to:external@evil.com)", rule.ToGmailQuery());
    }

    // =========================================================================
    // 5. Negative & Empty Cases (Must Throw QueryValidationException)
    // =========================================================================

    [Fact]
    public async Task Not_EmptyCall_ThrowsValidationException()
    {
        string lua = "return not()";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Fact]
    public async Task Not_EmptyTable_ThrowsValidationException()
    {
        string lua = "return not({})";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Fact]
    public async Task Not_EmptyString_ThrowsValidationException()
    {
        string luaEmpty = "return not('')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaEmpty));

        string luaWhitespace = "return not('   ')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaWhitespace));
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
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Fact]
    public async Task FilterBuilder_EmptyNot_ThrowsValidationException()
    {
        string lua = @"
return filter():from('corp@company.com'):Not():build()
";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }
}
