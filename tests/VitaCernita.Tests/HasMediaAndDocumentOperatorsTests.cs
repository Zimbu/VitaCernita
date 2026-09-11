using System.Threading.Tasks;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Filters.Validation;
using Xunit;

namespace VitaCernita.Tests;

public class HasMediaAndDocumentOperatorsTests
{
    private readonly GmailFilterLoader _loader = new();

    // =========================================================================
    // 1. Direct DSL Expressions (Callable and Values)
    // =========================================================================

    [Theory]
    [InlineData("return has_attachment", "has:attachment")]
    [InlineData("return has_attachment()", "has:attachment")]
    [InlineData("return HasAttachment()", "has:attachment")]
    [InlineData("return has('attachment')", "has:attachment")]
    [InlineData("return has_drive", "has:drive")]
    [InlineData("return has_drive()", "has:drive")]
    [InlineData("return has('drive')", "has:drive")]
    [InlineData("return has_document", "has:document")]
    [InlineData("return has_document()", "has:document")]
    [InlineData("return has('document')", "has:document")]
    [InlineData("return has_spreadsheet", "has:spreadsheet")]
    [InlineData("return has_spreadsheet()", "has:spreadsheet")]
    [InlineData("return has('spreadsheet')", "has:spreadsheet")]
    [InlineData("return has_presentation", "has:presentation")]
    [InlineData("return has_presentation()", "has:presentation")]
    [InlineData("return has('presentation')", "has:presentation")]
    [InlineData("return has_youtube", "has:youtube")]
    [InlineData("return has_youtube()", "has:youtube")]
    [InlineData("return has('youtube')", "has:youtube")]
    [InlineData("return has_user_labels", "has:userlabels")]
    [InlineData("return has_user_labels()", "has:userlabels")]
    [InlineData("return HasUserLabels", "has:userlabels")]
    [InlineData("return user_labels", "has:userlabels")]
    [InlineData("return has('userlabels')", "has:userlabels")]
    [InlineData("return has('user_labels')", "has:userlabels")]
    [InlineData("return has('user-labels')", "has:userlabels")]
    [InlineData("return has_no_user_labels", "has:nouserlabels")]
    [InlineData("return has_no_user_labels()", "has:nouserlabels")]
    [InlineData("return HasNoUserLabels", "has:nouserlabels")]
    [InlineData("return no_user_labels", "has:nouserlabels")]
    [InlineData("return has('nouserlabels')", "has:nouserlabels")]
    [InlineData("return has('no_user_labels')", "has:nouserlabels")]
    [InlineData("return has('no-user-labels')", "has:nouserlabels")]
    public async Task Has_MediaAndMetadata_EmitsCorrectQuery(string luaScript, string expectedQuery)
    {
        var rule = await _loader.LoadRuleFromScriptAsync(luaScript);
        Assert.Equal(expectedQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Has_Aliases_NormalizeCorrectly()
    {
        var scriptUserLabels = "return has('user-labels')";
        var ruleUserLabels = await _loader.LoadRuleFromScriptAsync(scriptUserLabels);
        Assert.Equal("has:userlabels", ruleUserLabels.ToGmailQuery());

        var scriptNoUserLabels = "return has('no-user-labels')";
        var ruleNoUserLabels = await _loader.LoadRuleFromScriptAsync(scriptNoUserLabels);
        Assert.Equal("has:nouserlabels", ruleNoUserLabels.ToGmailQuery());

        var scriptYouTube = "return has('you-tube')";
        var ruleYouTube = await _loader.LoadRuleFromScriptAsync(scriptYouTube);
        Assert.Equal("has:youtube", ruleYouTube.ToGmailQuery());
    }

    // =========================================================================
    // 2. Declarative Table Syntax
    // =========================================================================

    [Fact]
    public async Task TableSyntax_BooleanKeys_EmitHasQueries()
    {
        string lua = @"
return {
    from = 'finance@company.com',
    has_attachment = true,
    has_drive = true
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:finance@company.com has:attachment has:drive", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_ShortBooleanKey_EmitsHasAttachment()
    {
        string lua = @"
return {
    from = 'contracts@company.com',
    attachment = true
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:contracts@company.com has:attachment", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_UserLabels_EmitsHasUserLabels()
    {
        string lua = @"
return {
    from = 'contracts@company.com',
    has_user_labels = true
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:contracts@company.com has:userlabels", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_NoUserLabels_EmitsHasNoUserLabels()
    {
        string lua = @"
return {
    from = 'inbox@company.com',
    has_no_user_labels = true
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:inbox@company.com has:nouserlabels", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_HasString_EmitsCorrectQuery()
    {
        string lua = @"
return {
    from = 'video@company.com',
    has = 'youtube'
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:video@company.com has:youtube", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_HasArrayOfStrings_EmitsAllHasConditions()
    {
        string lua = @"
return {
    from = 'shared@company.com',
    has = { 'document', 'spreadsheet', 'presentation' }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:shared@company.com has:document has:presentation has:spreadsheet", rule.ToGmailQuery());
    }

    // =========================================================================
    // 3. Logical Operator Combinations (And, Or, Not)
    // =========================================================================

    [Fact]
    public async Task Logical_AndWithMediaHas_EmitsCorrectQuery()
    {
        string lua = @"
return And(
    From('boss@company.com'),
    Subject('Q3 Numbers'),
    has_spreadsheet
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:boss@company.com subject:\"Q3 Numbers\" has:spreadsheet", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Logical_OrWithMediaHas_EmitsCorrectQuery()
    {
        string lua = @"
return Or(
    has_drive,
    has_document,
    has_spreadsheet
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("has:document OR has:drive OR has:spreadsheet", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Logical_NotWithMediaHas_EmitsCorrectQuery()
    {
        string lua = "return not(has_attachment)";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-has:attachment", rule.ToGmailQuery());

        string luaCompound = "return not(Or(has_user_labels, has_drive))";
        var ruleCompound = await _loader.LoadRuleFromScriptAsync(luaCompound);
        Assert.Equal("-(has:drive OR has:userlabels)", ruleCompound.ToGmailQuery());
    }

    // =========================================================================
    // 4. FilterBuilder Chaining
    // =========================================================================

    [Fact]
    public async Task FilterBuilder_MediaHasMethods_ChainsCorrectly()
    {
        string lua = @"
return filter()
    :from('admin@company.com')
    :has_attachment()
    :has_drive()
    :has_youtube()
    :build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:admin@company.com has:attachment has:drive has:youtube", rule.ToGmailQuery());
    }

    [Fact]
    public async Task FilterBuilder_UserLabels_ChainsCorrectly()
    {
        string lua = @"
return filter()
    :from('admin@company.com')
    :has_user_labels()
    :build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:admin@company.com has:userlabels", rule.ToGmailQuery());

        string luaNo = @"
return filter()
    :from('admin@company.com')
    :has_no_user_labels()
    :build()
";
        var ruleNo = await _loader.LoadRuleFromScriptAsync(luaNo);
        Assert.Equal("from:admin@company.com has:nouserlabels", ruleNo.ToGmailQuery());
    }

    // =========================================================================
    // 5. Input Validation
    // =========================================================================

    [Fact]
    public async Task Validation_InvalidHasTarget_ThrowsQueryValidationException()
    {
        string lua = "return has('invalid-format')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Fact]
    public async Task Validation_EmptyHasTarget_ThrowsQueryValidationException()
    {
        string luaEmpty = "return has('')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaEmpty));

        string luaWhitespace = "return has('   ')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaWhitespace));
    }
}
