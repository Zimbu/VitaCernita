using System.Threading.Tasks;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Filters.Validation;
using Xunit;

namespace VitaCernita.Tests;

public class InLocationOperatorsTests
{
    private readonly GmailFilterLoader _loader = new();

    // =========================================================================
    // 1. Direct DSL Expressions (Callable and Values)
    // =========================================================================

    [Theory]
    [InlineData("return in_anywhere", "in:anywhere")]
    [InlineData("return in_anywhere()", "in:anywhere")]
    [InlineData("return anywhere", "in:anywhere")]
    [InlineData("return In('anywhere')", "in:anywhere")]
    [InlineData("return in('anywhere')", "in:anywhere")]
    [InlineData("return in_folder('anywhere')", "in:anywhere")]
    [InlineData("return in_archive", "in:archive")]
    [InlineData("return in_archive()", "in:archive")]
    [InlineData("return archive", "in:archive")]
    [InlineData("return in('archive')", "in:archive")]
    [InlineData("return in_snoozed", "in:snoozed")]
    [InlineData("return in_snoozed()", "in:snoozed")]
    [InlineData("return in('snoozed')", "in:snoozed")]
    [InlineData("return in_inbox", "in:inbox")]
    [InlineData("return in_inbox()", "in:inbox")]
    [InlineData("return inbox", "in:inbox")]
    [InlineData("return in('inbox')", "in:inbox")]
    [InlineData("return in_sent", "in:sent")]
    [InlineData("return in_sent()", "in:sent")]
    [InlineData("return in('sent')", "in:sent")]
    [InlineData("return in_drafts", "in:drafts")]
    [InlineData("return in_drafts()", "in:drafts")]
    [InlineData("return in_draft", "in:drafts")]
    [InlineData("return drafts", "in:drafts")]
    [InlineData("return in('drafts')", "in:drafts")]
    [InlineData("return in('draft')", "in:drafts")]
    [InlineData("return in_trash", "in:trash")]
    [InlineData("return in_trash()", "in:trash")]
    [InlineData("return trash", "in:trash")]
    [InlineData("return in('trash')", "in:trash")]
    [InlineData("return in_spam", "in:spam")]
    [InlineData("return in_spam()", "in:spam")]
    [InlineData("return spam", "in:spam")]
    [InlineData("return in('spam')", "in:spam")]
    [InlineData("return in_chats", "in:chats")]
    [InlineData("return in_chats()", "in:chats")]
    [InlineData("return in_chat", "in:chats")]
    [InlineData("return chats", "in:chats")]
    [InlineData("return in('chats')", "in:chats")]
    [InlineData("return in('chat')", "in:chats")]
    public async Task In_LocationAndFolder_EmitsCorrectQuery(string luaScript, string expectedQuery)
    {
        var rule = await _loader.LoadRuleFromScriptAsync(luaScript);
        Assert.Equal(expectedQuery, rule.ToGmailQuery());
    }

    // =========================================================================
    // 2. Declarative Table Syntax
    // =========================================================================

    [Fact]
    public async Task TableSyntax_InKey_EmitsCorrectQuery()
    {
        string lua = @"
return {
    from = 'contracts@company.com',
    ['in'] = 'anywhere'
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:contracts@company.com in:anywhere", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_BooleanKeys_EmitInQueries()
    {
        string lua = @"
return {
    from = 'finance@company.com',
    in_archive = true
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:finance@company.com in:archive", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_InArrayOfStrings_EmitsAllInConditions()
    {
        string lua = @"
return {
    from = 'finance@company.com',
    ['in'] = { 'inbox', 'archive' }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:finance@company.com in:archive in:inbox", rule.ToGmailQuery());
    }

    // =========================================================================
    // 3. Logical Operator Combinations (And, Or, Not)
    // =========================================================================

    [Fact]
    public async Task Logical_AndWithInOperator_EmitsCorrectQuery()
    {
        string lua = @"
return And(
    From('boss@example.com'),
    in_inbox
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:boss@example.com in:inbox", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Logical_OrWithInOperator_EmitsCorrectQuery()
    {
        string lua = @"
return Or(
    in_inbox,
    in_archive
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("in:archive OR in:inbox", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Logical_NotWithInOperator_EmitsCorrectQuery()
    {
        string lua = "return not(in_trash)";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-in:trash", rule.ToGmailQuery());

        string luaCompound = "return not(Or(in_trash, in_spam))";
        var ruleCompound = await _loader.LoadRuleFromScriptAsync(luaCompound);
        Assert.Equal("-(in:spam OR in:trash)", ruleCompound.ToGmailQuery());
    }

    // =========================================================================
    // 4. FilterBuilder Chaining
    // =========================================================================

    [Fact]
    public async Task FilterBuilder_InMethods_ChainsCorrectly()
    {
        string lua = @"
return filter()
    :from('admin@company.com')
    :in_anywhere()
    :build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:admin@company.com in:anywhere", rule.ToGmailQuery());
    }

    [Fact]
    public async Task FilterBuilder_InDirectMethod_ChainsCorrectly()
    {
        string lua = @"
return filter()
    :from('admin@company.com')
    :In('archive')
    :build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:admin@company.com in:archive", rule.ToGmailQuery());
    }

    // =========================================================================
    // 5. Input Validation
    // =========================================================================

    [Fact]
    public async Task Validation_InvalidInTarget_ThrowsQueryValidationException()
    {
        string lua = "return in('nonexistent_folder')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Fact]
    public async Task Validation_EmptyInTarget_ThrowsQueryValidationException()
    {
        string luaEmpty = "return in('')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaEmpty));

        string luaWhitespace = "return in('   ')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaWhitespace));
    }
}
