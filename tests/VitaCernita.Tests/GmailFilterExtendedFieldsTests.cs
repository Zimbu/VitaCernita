using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Filters;

namespace VitaCernita.Tests;

/// <summary>
/// Unit tests for extended string match fields:
/// to, cc, bcc, list, filename, delivered-to, rfc822msgid, header, match (exact phrase),
/// and discovered operators (label, category, has, is, in).
/// </summary>
public class GmailFilterExtendedFieldsTests
{
    private readonly GmailFilterLoader _loader = new();

    [Fact]
    public async Task Parse_ToField_ProducesCorrectFilter()
    {
        string lua = @"return To('devs@example.com')";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("to:devs@example.com", rule.ToGmailQuery());

        string luaSpaces = @"return to('Core Team <devs@example.com>')";
        var ruleSpaces = await _loader.LoadRuleFromScriptAsync(luaSpaces);
        Assert.Equal("to:\"Core Team <devs@example.com>\"", ruleSpaces.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_CcAndBccFields_ProducesCorrectFilter()
    {
        string lua = @"
return And(
    Cc('audit@example.com'),
    Bcc('archive@example.com')
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("cc:audit@example.com bcc:archive@example.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_ListField_ProducesCorrectFilter()
    {
        string lua = @"return List('announce@lists.example.com')";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("list:announce@lists.example.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_FilenameField_ProducesCorrectFilter()
    {
        string luaSimple = @"return Filename('invoice.pdf')";
        var ruleSimple = await _loader.LoadRuleFromScriptAsync(luaSimple);
        Assert.Equal("filename:invoice.pdf", ruleSimple.ToGmailQuery());

        string luaSpaces = @"return filename('monthly report.pdf')";
        var ruleSpaces = await _loader.LoadRuleFromScriptAsync(luaSpaces);
        Assert.Equal("filename:\"monthly report.pdf\"", ruleSpaces.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_DeliveredToField_NormalizesToDeliveredToOperator()
    {
        // Functional DSL: delivered_to
        string luaFn = @"return delivered_to('alias@company.com')";
        var ruleFn = await _loader.LoadRuleFromScriptAsync(luaFn);
        Assert.Equal("deliveredto:alias@company.com", ruleFn.ToGmailQuery());

        // Declarative table with hyphenated delivered-to
        string luaTable = @"
return {
    ['delivered-to'] = 'alias@company.com'
}
";
        var ruleTable = await _loader.LoadRuleFromScriptAsync(luaTable);
        Assert.Equal("deliveredto:alias@company.com", ruleTable.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_Rfc822MsgId_ProducesCorrectFilter()
    {
        string lua = @"return rfc822msgid('200503292@example.com')";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("rfc822msgid:200503292@example.com", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_HeaderField_SupportsStringAndKeyValue()
    {
        // 1. Full header string per Google documentation: header:X-Google-Calendar-Notification:rsvpWithNote
        string luaFull = @"return Header('X-Google-Calendar-Notification:rsvpWithNote')";
        var ruleFull = await _loader.LoadRuleFromScriptAsync(luaFull);
        Assert.Equal("header:X-Google-Calendar-Notification:rsvpWithNote", ruleFull.ToGmailQuery());

        // 2. Two arguments: header(name, value)
        string luaTwoArgs = @"return header('X-Spam-Flag', 'YES')";
        var ruleTwoArgs = await _loader.LoadRuleFromScriptAsync(luaTwoArgs);
        Assert.Equal("header:X-Spam-Flag:YES", ruleTwoArgs.ToGmailQuery());

        // 3. Header with spaces in value
        string luaSpaces = @"return header('X-Custom-Env', 'production environment')";
        var ruleSpaces = await _loader.LoadRuleFromScriptAsync(luaSpaces);
        Assert.Equal("header:X-Custom-Env:\"production environment\"", ruleSpaces.ToGmailQuery());

        // 4. Declarative table with header subtable
        string luaTable = @"
return {
    header = {
        name = 'X-Git-Event',
        value = 'push'
    }
}
";
        var ruleTable = await _loader.LoadRuleFromScriptAsync(luaTable);
        Assert.Equal("header:X-Git-Event:push", ruleTable.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_MatchExactPhrase_AlwaysDoubleQuotedPerGoogleDocumentation()
    {
        // Google help doc: "Search for emails with an exact word or phrase: 'dinner and movie tonight'"
        string luaPhrase = @"return match('dinner and movie tonight')";
        var rulePhrase = await _loader.LoadRuleFromScriptAsync(luaPhrase);
        Assert.Equal("\"dinner and movie tonight\"", rulePhrase.ToGmailQuery());

        // Exact single-word match
        string luaWord = @"return Match('urgent')";
        var ruleWord = await _loader.LoadRuleFromScriptAsync(luaWord);
        Assert.Equal("\"urgent\"", ruleWord.ToGmailQuery());

        // In combination with other fields
        string luaCombined = @"
return And(
    From('boss@example.com'),
    match('board meeting notes')
)
";
        var ruleCombined = await _loader.LoadRuleFromScriptAsync(luaCombined);
        Assert.Equal("from:boss@example.com \"board meeting notes\"", ruleCombined.ToGmailQuery());

        // Declarative table style
        string luaTable = @"
return {
    from = 'boss@example.com',
    match = 'board meeting notes'
}
";
        var ruleTable = await _loader.LoadRuleFromScriptAsync(luaTable);
        Assert.Equal("from:boss@example.com \"board meeting notes\"", ruleTable.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_DiscoveredGoogleOperators_ProducesCorrectFilter()
    {
        string lua = @"
return And(
    Label('finance'),
    Category('promotions'),
    Has('attachment'),
    Is('unread'),
    in_folder('archive')
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("label:finance category:promotions has:attachment is:unread in:archive", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_ComprehensiveCombinationOfNewFields_CanonicalOrder()
    {
        string lua = @"
return And(
    match('confidential audit'),
    Filename('report.pdf'),
    Subject('Q3 Financial Review'),
    To('finance@company.com'),
    Header('X-Security', 'High'),
    From('cfo@company.com'),
    Cc('ceo@company.com'),
    Bcc('legal@company.com'),
    delivered_to('auditor@company.com'),
    List('c-suite@company.com'),
    rfc822msgid('msg-987@company.com')
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        string expected = "from:cfo@company.com " +
                          "to:finance@company.com " +
                          "cc:ceo@company.com " +
                          "bcc:legal@company.com " +
                          "deliveredto:auditor@company.com " +
                          "subject:\"Q3 Financial Review\" " +
                          "list:c-suite@company.com " +
                          "filename:report.pdf " +
                          "header:X-Security:High " +
                          "rfc822msgid:msg-987@company.com " +
                          "\"confidential audit\"";

        Assert.Equal(expected, rule.ToGmailQuery());
    }

    [Fact]
    public async Task Parse_NestedOrWithExtendedFields_ProducesCorrectFilter()
    {
        string lua = @"
return Or(
    And(From('alerts@example.com'), Header('X-Severity', 'CRITICAL')),
    And(To('security@example.com'), match('unauthorized access')),
    Filename('dump.core')
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        string expected = "filename:dump.core OR " +
                          "(from:alerts@example.com header:X-Severity:CRITICAL) OR " +
                          "(to:security@example.com \"unauthorized access\")";

        Assert.Equal(expected, rule.ToGmailQuery());
    }
}
