using System.Threading.Tasks;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Filters.Validation;
using Xunit;

namespace VitaCernita.Tests;

public class StarOperatorsTests
{
    private readonly GmailFilterLoader _loader = new();

    // =========================================================================
    // 1. All 12 Stars and Icons (Function Call Syntax)
    // =========================================================================

    [Theory]
    [InlineData("has_yellow_star()", "has:yellow-star")]
    [InlineData("has_orange_star()", "has:orange-star")]
    [InlineData("has_red_star()", "has:red-star")]
    [InlineData("has_purple_star()", "has:purple-star")]
    [InlineData("has_blue_star()", "has:blue-star")]
    [InlineData("has_green_star()", "has:green-star")]
    [InlineData("has_red_bang()", "has:red-bang")]
    [InlineData("has_yellow_bang()", "has:yellow-bang")]
    [InlineData("has_orange_guillemet()", "has:orange-guillemet")]
    [InlineData("has_green_check()", "has:green-check")]
    [InlineData("has_blue_info()", "has:blue-info")]
    [InlineData("has_purple_question()", "has:purple-question")]
    public async Task StarOperators_FunctionCall_EmitOfficialHyphenatedQuery(string expr, string expected)
    {
        string lua = $"return {expr}";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(expected, rule.ToGmailQuery());
    }

    // =========================================================================
    // 2. All 12 Stars and Icons (Constant / Identifier Syntax without Parens)
    // =========================================================================

    [Theory]
    [InlineData("has_yellow_star", "has:yellow-star")]
    [InlineData("has_orange_star", "has:orange-star")]
    [InlineData("has_red_star", "has:red-star")]
    [InlineData("has_purple_star", "has:purple-star")]
    [InlineData("has_blue_star", "has:blue-star")]
    [InlineData("has_green_star", "has:green-star")]
    [InlineData("has_red_bang", "has:red-bang")]
    [InlineData("has_yellow_bang", "has:yellow-bang")]
    [InlineData("has_orange_guillemet", "has:orange-guillemet")]
    [InlineData("has_green_check", "has:green-check")]
    [InlineData("has_blue_info", "has:blue-info")]
    [InlineData("has_purple_question", "has:purple-question")]
    public async Task StarOperators_ConstantSyntax_EmitOfficialHyphenatedQuery(string expr, string expected)
    {
        string lua = $"return {expr}";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(expected, rule.ToGmailQuery());
    }

    // =========================================================================
    // 3. PascalCase Identifiers
    // =========================================================================

    [Theory]
    [InlineData("HasYellowStar", "has:yellow-star")]
    [InlineData("HasOrangeStar", "has:orange-star")]
    [InlineData("HasRedStar", "has:red-star")]
    [InlineData("HasPurpleStar", "has:purple-star")]
    [InlineData("HasBlueStar", "has:blue-star")]
    [InlineData("HasGreenStar", "has:green-star")]
    [InlineData("HasRedBang", "has:red-bang")]
    [InlineData("HasYellowBang", "has:yellow-bang")]
    [InlineData("HasOrangeGuillemet", "has:orange-guillemet")]
    [InlineData("HasGreenCheck", "has:green-check")]
    [InlineData("HasBlueInfo", "has:blue-info")]
    [InlineData("HasPurpleQuestion", "has:purple-question")]
    public async Task StarOperators_PascalCase_EmitOfficialHyphenatedQuery(string expr, string expected)
    {
        string lua = $"return {expr}";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(expected, rule.ToGmailQuery());
    }

    // =========================================================================
    // 4. Generic has(...) Operator with Underscores and Hyphens
    // =========================================================================

    [Theory]
    [InlineData("yellow_star", "has:yellow-star")]
    [InlineData("yellow-star", "has:yellow-star")]
    [InlineData("red_bang", "has:red-bang")]
    [InlineData("red-bang", "has:red-bang")]
    [InlineData("orange_guillemet", "has:orange-guillemet")]
    [InlineData("orange-guillemet", "has:orange-guillemet")]
    [InlineData("orange_guillemets", "has:orange-guillemet")]
    [InlineData("green_check", "has:green-check")]
    [InlineData("green-check", "has:green-check")]
    [InlineData("blue_info", "has:blue-info")]
    [InlineData("blue-info", "has:blue-info")]
    [InlineData("purple_question", "has:purple-question")]
    [InlineData("purple-question", "has:purple-question")]
    [InlineData("RED-BANG", "has:red-bang")]
    public async Task GenericHas_ValidStarInputs_EmitNormalizedHyphenatedQuery(string input, string expected)
    {
        string lua = $"return has('{input}')";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal(expected, rule.ToGmailQuery());
    }

    // =========================================================================
    // 5. FilterBuilder Methods
    // =========================================================================

    [Fact]
    public async Task FilterBuilder_StarMethods_BuildCorrectQuery()
    {
        string lua = @"
return filter():from('manager@company.com'):has_red_bang():subject('Urgent Escalation'):build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:manager@company.com subject:\"Urgent Escalation\" has:red-bang", rule.ToGmailQuery());
    }

    [Fact]
    public async Task FilterBuilder_GenericHasMethod_BuildCorrectQuery()
    {
        string lua = @"
return filter():has('orange-guillemet'):label('alerts'):build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("label:alerts has:orange-guillemet", rule.ToGmailQuery());
    }

    // =========================================================================
    // 6. Declarative Table Syntax
    // =========================================================================

    [Fact]
    public async Task TableSyntax_BooleanStarKeys_EmitCorrectQuery()
    {
        string lua = @"
return {
    from = 'audit@company.com',
    has_yellow_star = true
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:audit@company.com has:yellow-star", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_HasStringKey_EmitCorrectQuery()
    {
        string lua = @"
return {
    has = 'purple_question',
    to = 'helpdesk@company.com'
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("to:helpdesk@company.com has:purple-question", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_HasTableValue_EmitCorrectQuery()
    {
        string lua = @"
return {
    from = 'ciso@company.com',
    has = has_blue_info
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:ciso@company.com has:blue-info", rule.ToGmailQuery());
    }

    // =========================================================================
    // 7. Composite Logic (Official Google Documentation Examples)
    // =========================================================================

    [Fact]
    public async Task CompositeLogic_OfficialGoogleExample_YellowStarOrPurpleQuestion()
    {
        // Google search operators example: has:yellow-star OR has:purple-question (alphabetically sorted by OrCondition)
        string lua = @"
return Or(has_yellow_star, has_purple_question)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("has:purple-question OR has:yellow-star", rule.ToGmailQuery());
    }

    [Fact]
    public async Task CompositeLogic_AndWithOrOfStars()
    {
        string lua = @"
return And(
    From('manager@example.com'),
    Or(has_red_bang, has_yellow_bang)
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:manager@example.com (has:red-bang OR has:yellow-bang)", rule.ToGmailQuery());
    }

    // =========================================================================
    // 8. Input Validation
    // =========================================================================

    [Theory]
    [InlineData("black-star")]
    [InlineData("white-star")]
    [InlineData("gold-star")]
    [InlineData("star")]
    [InlineData("bang")]
    [InlineData("unknown_icon")]
    [InlineData("123")]
    public async Task GenericHas_InvalidStarName_ThrowsValidationException(string invalidStar)
    {
        string lua = $"return has('{invalidStar}')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenericHas_EmptyOrWhitespace_ThrowsValidationException(string emptyValue)
    {
        string lua = $"return has('{emptyValue}')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    // =========================================================================
    // 9. is_starred & is:starred Operators
    // =========================================================================

    [Theory]
    [InlineData("is_starred")]
    [InlineData("is_starred()")]
    [InlineData("IsStarred")]
    [InlineData("starred")]
    [InlineData("is('starred')")]
    [InlineData("is('STARRED')")]
    public async Task IsStarred_VariousSyntaxForms_EmitIsStarred(string expr)
    {
        string lua = $"return {expr}";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("is:starred", rule.ToGmailQuery());
    }

    [Fact]
    public async Task IsStarred_InAndComposition_EmitsCorrectQuery()
    {
        string lua = @"
return And(
    From('vip@company.com'),
    is_starred
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:vip@company.com is:starred", rule.ToGmailQuery());
    }

    [Fact]
    public async Task IsStarred_InFilterBuilder_EmitsCorrectQuery()
    {
        string lua = @"
return filter():from('exec@company.com'):is_starred():build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:exec@company.com is:starred", rule.ToGmailQuery());
    }

    [Fact]
    public async Task IsStarred_InTableSyntax_EmitsCorrectQuery()
    {
        string lua = @"
return {
    from = 'finance@company.com',
    is_starred = true
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:finance@company.com is:starred", rule.ToGmailQuery());
    }

    [Fact]
    public async Task IsStarred_InTableSyntaxWithString_EmitsCorrectQuery()
    {
        string lua = @"
return {
    from = 'finance@company.com',
    ['is'] = 'starred'
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:finance@company.com is:starred", rule.ToGmailQuery());
    }

    [Theory]
    [InlineData("important")] // not yet enabled in this commit
    [InlineData("unread")]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenericIs_UnsupportedOrEmptyTarget_ThrowsValidationException(string target)
    {
        string lua = $"return is('{target}')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }
}
