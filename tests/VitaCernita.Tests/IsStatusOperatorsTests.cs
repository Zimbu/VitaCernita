using System.Threading.Tasks;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Filters.Validation;
using Xunit;

namespace VitaCernita.Tests;

public class IsStatusOperatorsTests
{
    private readonly GmailFilterLoader _loader = new();

    // =========================================================================
    // 1. Direct DSL Expressions (Callable and Values)
    // =========================================================================

    [Theory]
    [InlineData("return is_unread", "is:unread")]
    [InlineData("return is_unread()", "is:unread")]
    [InlineData("return unread", "is:unread")]
    [InlineData("return is('unread')", "is:unread")]
    [InlineData("return is_read", "is:read")]
    [InlineData("return is_read()", "is:read")]
    [InlineData("return read", "is:read")]
    [InlineData("return is('read')", "is:read")]
    [InlineData("return is_important", "is:important")]
    [InlineData("return is_important()", "is:important")]
    [InlineData("return important", "is:important")]
    [InlineData("return is('important')", "is:important")]
    [InlineData("return is_muted", "is:muted")]
    [InlineData("return is_muted()", "is:muted")]
    [InlineData("return muted", "is:muted")]
    [InlineData("return is('muted')", "is:muted")]
    [InlineData("return is_snoozed", "is:snoozed")]
    [InlineData("return is_snoozed()", "is:snoozed")]
    [InlineData("return snoozed", "is:snoozed")]
    [InlineData("return is('snoozed')", "is:snoozed")]
    [InlineData("return is_chat", "is:chat")]
    [InlineData("return is_chat()", "is:chat")]
    [InlineData("return chat", "is:chat")]
    [InlineData("return is('chat')", "is:chat")]
    [InlineData("return is_draft", "is:draft")]
    [InlineData("return is_draft()", "is:draft")]
    [InlineData("return draft", "is:draft")]
    [InlineData("return is('draft')", "is:draft")]
    [InlineData("return is_sent", "is:sent")]
    [InlineData("return is_sent()", "is:sent")]
    [InlineData("return is('sent')", "is:sent")]
    [InlineData("return is_trash", "is:trash")]
    [InlineData("return is_trash()", "is:trash")]
    [InlineData("return is('trash')", "is:trash")]
    [InlineData("return is_spam", "is:spam")]
    [InlineData("return is_spam()", "is:spam")]
    [InlineData("return is('spam')", "is:spam")]
    public async Task Is_StatusAndState_EmitsCorrectQuery(string luaScript, string expectedQuery)
    {
        var rule = await _loader.LoadRuleFromScriptAsync(luaScript);
        Assert.Equal(expectedQuery, rule.ToGmailQuery());
    }

    // =========================================================================
    // 2. Declarative Table Syntax
    // =========================================================================

    [Fact]
    public async Task TableSyntax_BooleanKeys_EmitIsQueries()
    {
        string lua = @"
return {
    from = 'finance@company.com',
    unread = true,
    important = true
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:finance@company.com is:important is:unread", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_PrefixedBooleanKeys_EmitIsQueries()
    {
        string lua = @"
return {
    from = 'finance@company.com',
    is_muted = true,
    is_snoozed = true
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:finance@company.com is:muted is:snoozed", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_IsString_EmitsCorrectQuery()
    {
        string lua = @"
return {
    from = 'security@company.com',
    is = 'unread'
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:security@company.com is:unread", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_IsArrayOfStrings_EmitsAllIsConditions()
    {
        string lua = @"
return {
    from = 'support@company.com',
    is = { 'important', 'unread' }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:support@company.com is:important is:unread", rule.ToGmailQuery());
    }

    // =========================================================================
    // 3. Logical Operator Combinations (And, Or, Not)
    // =========================================================================

    [Fact]
    public async Task Logical_AndWithIsOperators_EmitsCorrectQuery()
    {
        string lua = @"
return And(
    From('boss@example.com'),
    is_important,
    is_unread
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:boss@example.com is:important is:unread", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Logical_OrWithIsOperators_EmitsCorrectQuery()
    {
        string lua = @"
return Or(
    is_starred,
    is_important,
    is_unread
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("is:important OR is:starred OR is:unread", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Logical_NotWithIsOperators_EmitsCorrectQuery()
    {
        string lua = "return not(is_read)";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-is:read", rule.ToGmailQuery());

        string luaCompound = "return not(Or(is_muted, is_spam))";
        var ruleCompound = await _loader.LoadRuleFromScriptAsync(luaCompound);
        Assert.Equal("-(is:muted OR is:spam)", ruleCompound.ToGmailQuery());
    }

    // =========================================================================
    // 4. FilterBuilder Chaining
    // =========================================================================

    [Fact]
    public async Task FilterBuilder_IsMethods_ChainsCorrectly()
    {
        string lua = @"
return filter()
    :from('admin@company.com')
    :is_unread()
    :is_important()
    :build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:admin@company.com is:important is:unread", rule.ToGmailQuery());
    }

    // =========================================================================
    // 5. Input Validation
    // =========================================================================

    [Fact]
    public async Task Validation_InvalidIsTarget_ThrowsFilterValidationException()
    {
        string lua = "return is('nonexistent_state')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Fact]
    public async Task Validation_EmptyIsTarget_ThrowsFilterValidationException()
    {
        string luaEmpty = "return is('')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(luaEmpty));

        string luaWhitespace = "return is('   ')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(luaWhitespace));
    }
}
