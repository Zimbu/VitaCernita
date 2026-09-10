using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Filters;

namespace VitaCernita.Tests;

/// <summary>
/// Boundary Value Analysis (BVA) and Nesting Test Cases for AND/OR Gmail filters.
/// </summary>
public class GmailFilterNestingTests
{
    private readonly GmailFilterLoader _loader = new();

    // =========================================================================
    // Core Requirement: OR containing three different matches of AND operations
    // =========================================================================

    [Fact]
    public async Task Or_ContainingThreeMatchesOfAnd_FunctionalDsl_ProducesCorrectFilter()
    {
        string lua = @"
return Or(
    And(From('alice@example.com'), Subject('Incident')),
    And(From('bob@example.com'), Subject('Deployment')),
    And(From('carol@example.com'), Subject('Payroll'))
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        string expected = "(from:alice@example.com subject:Incident) OR " +
                          "(from:bob@example.com subject:Deployment) OR " +
                          "(from:carol@example.com subject:Payroll)";

        Assert.Equal(expected, rule.ToGmailQuery());

        string expectedExplicit = "(from:alice@example.com AND subject:Incident) OR " +
                                  "(from:bob@example.com AND subject:Deployment) OR " +
                                  "(from:carol@example.com AND subject:Payroll)";
        Assert.Equal(expectedExplicit, rule.ToGmailQuery(explicitAnd: true));
    }

    [Fact]
    public async Task Or_ContainingThreeMatchesOfAnd_DeclarativeTable_ProducesIdenticalFilter()
    {
        string lua = @"
return {
    ['or'] = {
        { from = 'alice@example.com', subject = 'Incident' },
        { from = 'bob@example.com', subject = 'Deployment' },
        { from = 'carol@example.com', subject = 'Payroll' }
    }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        string expected = "(from:alice@example.com subject:Incident) OR " +
                          "(from:bob@example.com subject:Deployment) OR " +
                          "(from:carol@example.com subject:Payroll)";

        Assert.Equal(expected, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Or_ContainingThreeMatchesOfAnd_OrderPermutations_ProducesCanonicalFilter()
    {
        // Reordering the branches (carol, then bob, then alice) must produce the exact same canonical string
        string lua = @"
return Or(
    And(From('carol@example.com'), Subject('Payroll')),
    And(From('bob@example.com'), Subject('Deployment')),
    And(From('alice@example.com'), Subject('Incident'))
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        string expected = "(from:alice@example.com subject:Incident) OR " +
                          "(from:bob@example.com subject:Deployment) OR " +
                          "(from:carol@example.com subject:Payroll)";

        Assert.Equal(expected, rule.ToGmailQuery());
    }

    // =========================================================================
    // Core Requirement: AND containing OR
    // =========================================================================

    [Fact]
    public async Task And_ContainingOr_FunctionalDsl_ProducesCorrectFilter()
    {
        string lua = @"
return And(
    From('boss@example.com'),
    Or(Subject('Urgent'), Subject('Critical'))
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        // Child OR inside AND must be wrapped in parentheses; conditions inside OR are sorted canonically
        string expected = "from:boss@example.com (subject:Critical OR subject:Urgent)";
        Assert.Equal(expected, rule.ToGmailQuery());

        string expectedExplicit = "from:boss@example.com AND (subject:Critical OR subject:Urgent)";
        Assert.Equal(expectedExplicit, rule.ToGmailQuery(explicitAnd: true));
    }

    [Fact]
    public async Task And_ContainingOr_DeclarativeTable_ProducesIdenticalFilter()
    {
        string lua = @"
return {
    from = 'boss@example.com',
    ['or'] = {
        { subject = 'Urgent' },
        { subject = 'Critical' }
    }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        string expected = "from:boss@example.com (subject:Critical OR subject:Urgent)";
        Assert.Equal(expected, rule.ToGmailQuery());
    }

    [Fact]
    public async Task And_ContainingMultipleOrs_ProducesCorrectFilter()
    {
        string lua = @"
return And(
    Or(From('alice@example.com'), From('bob@example.com')),
    Or(Subject('Urgent'), Subject('Critical'))
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        string expected = "(from:alice@example.com OR from:bob@example.com) (subject:Critical OR subject:Urgent)";
        Assert.Equal(expected, rule.ToGmailQuery());

        string expectedExplicit = "(from:alice@example.com OR from:bob@example.com) AND (subject:Critical OR subject:Urgent)";
        Assert.Equal(expectedExplicit, rule.ToGmailQuery(explicitAnd: true));
    }

    // =========================================================================
    // Boundary Value Analysis: Arity Boundaries (N = 1, N = 2, N = 4)
    // =========================================================================

    [Fact]
    public async Task Bva_Arity1_SingleConditionInOr_SimplifiesWithoutParensOrOperator()
    {
        // Boundary N = 1 for OR
        string lua = @"return Or(From('solo@example.com'))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        Assert.Equal("from:solo@example.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Bva_Arity1_NestedSingleInAnd_SimplifiesCleanly()
    {
        // Boundary N = 1 nested inside AND: Or with 1 item should not add parens
        string lua = @"return And(Or(From('solo@example.com')), Subject('Hello'))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        Assert.Equal("from:solo@example.com subject:Hello", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Bva_Arity2_MinimalOr_ProducesCorrectFilter()
    {
        // Boundary N = 2 for OR: two flat field conditions
        string lua = @"return Or(From('alerts@monitoring.com'), Subject('Critical'))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        Assert.Equal("from:alerts@monitoring.com OR subject:Critical", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Bva_Arity4_FourMatchesOfAndInsideOr_ScalesCorrectly()
    {
        // Boundary N = 4: scaling above nominal 3
        string lua = @"
return Or(
    And(From('a@test.com'), Subject('A')),
    And(From('b@test.com'), Subject('B')),
    And(From('c@test.com'), Subject('C')),
    And(From('d@test.com'), Subject('D'))
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        string expected = "(from:a@test.com subject:A) OR " +
                          "(from:b@test.com subject:B) OR " +
                          "(from:c@test.com subject:C) OR " +
                          "(from:d@test.com subject:D)";

        Assert.Equal(expected, rule.ToGmailQuery());
    }

    // =========================================================================
    // Boundary Value Analysis: Nesting Depth = 3 (Alternating Depth)
    // =========================================================================

    [Fact]
    public async Task Bva_Depth3_OrContainingAndContainingOr()
    {
        // OR -> AND -> OR
        string lua = @"
return Or(
    And(
        From('alice@example.com'),
        Or(Subject('Alpha'), Subject('Beta'))
    ),
    And(
        From('bob@example.com'),
        Subject('Gamma')
    )
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        string expected = "(from:alice@example.com (subject:Alpha OR subject:Beta)) OR " +
                          "(from:bob@example.com subject:Gamma)";

        Assert.Equal(expected, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Bva_Depth3_AndContainingOrContainingAnd()
    {
        // AND -> OR -> AND
        string lua = @"
return And(
    From('boss@example.com'),
    Or(
        And(From('ops@example.com'), Subject('Alert')),
        Subject('Emergency')
    )
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        string expected = "from:boss@example.com ((from:ops@example.com subject:Alert) OR subject:Emergency)";
        Assert.Equal(expected, rule.ToGmailQuery());
    }

    // =========================================================================
    // Boundary Value Analysis: Associative Flattening (Redundant Nesting)
    // =========================================================================

    [Fact]
    public async Task Bva_Flattening_NestedOrInsideOr_FlattensToSingleOr()
    {
        // Or(A, Or(B, C)) must flatten into A OR B OR C without redundant inner parentheses
        string lua = @"
return Or(
    From('a@test.com'),
    Or(
        From('b@test.com'),
        From('c@test.com')
    )
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        Assert.Equal("from:a@test.com OR from:b@test.com OR from:c@test.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Bva_Flattening_NestedAndInsideAnd_FlattensToSingleAnd()
    {
        // And(A, And(B, C)) must flatten into A B C
        string lua = @"
return And(
    From('a@test.com'),
    And(
        Subject('Meeting'),
        From('b@test.com')
    )
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        Assert.Equal("from:a@test.com from:b@test.com subject:Meeting", rule.ToGmailQuery());
    }

    // =========================================================================
    // Boundary Value Analysis: String Quoting in Nested Matches
    // =========================================================================

    [Fact]
    public async Task Bva_StringQuoting_NestedMatchesWithSpacesAndSingleWords()
    {
        string lua = @"
return Or(
    And(From('alice smith <alice@example.com>'), Subject('Q3 Financial Report')),
    And(From('bob@example.com'), Subject('Urgent'))
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        string expected = "(from:\"alice smith <alice@example.com>\" subject:\"Q3 Financial Report\") OR " +
                          "(from:bob@example.com subject:Urgent)";

        Assert.Equal(expected, rule.ToGmailQuery());
    }
}
