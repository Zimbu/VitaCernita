using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Queries;
using VitaCernita.Core.Serialization;
using Xunit;

namespace VitaCernita.Tests.Core.Filters;

public class GmailFilterImporterTests
{
    private readonly LuaConfigSerializer _serializer = LuaConfigSerializer.Default;
    private readonly GmailFilterLoader _loader = new();

    private static GmailFilter ImportFilterFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return GmailFilter.FromJsonElement(doc.RootElement);
    }

    // =========================================================================
    // 1. Structured Fields
    // =========================================================================

    [Fact]
    public async Task Import_SingleFromField_ProducesFromConditionAndValidLua()
    {
        string json = @"{
            ""id"": ""f1"",
            ""criteria"": { ""from"": ""alice@example.com"" },
            ""action"": { ""addLabelIds"": [""Label_1""] }
        }";

        var filter = ImportFilterFromJson(json);

        var field = Assert.IsType<FieldCondition>(filter.Query);
        Assert.Equal("from", field.Field);
        Assert.Equal("alice@example.com", field.Value);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("From(\"alice@example.com\")", lua);
        Assert.DoesNotContain("raw_query", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        Assert.Equal(filter.ToGmailQuery(), reloaded.ToGmailQuery());
    }

    [Fact]
    public async Task Import_MultipleStructuredFields_ProducesAndCondition()
    {
        string json = @"{
            ""id"": ""f2"",
            ""criteria"": {
                ""from"": ""billing@vendor.com"",
                ""to"": ""finance@mycorp.com"",
                ""subject"": ""Quarterly Report"",
                ""hasAttachment"": true,
                ""size"": 1048576,
                ""sizeComparison"": ""larger""
            },
            ""action"": { ""removeLabelIds"": [""INBOX""] }
        }";

        var filter = ImportFilterFromJson(json);

        var and = Assert.IsType<AndCondition>(filter.Query);
        Assert.Equal(5, and.Conditions.Count);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("From(\"billing@vendor.com\")", lua);
        Assert.Contains("To(\"finance@mycorp.com\")", lua);
        Assert.Contains("Subject(\"Quarterly Report\")", lua);
        Assert.Contains("has_attachment", lua);
        Assert.Contains("larger(\"1048576\")", lua);
        Assert.DoesNotContain("raw_query", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        Assert.Equal(filter.ToGmailQuery(), reloaded.ToGmailQuery());
    }

    // =========================================================================
    // 2. Not(), Not(And()), Not(Or())
    // =========================================================================

    [Fact]
    public async Task Import_NegatedQuerySingleWord_ProducesNotField()
    {
        string json = @"{
            ""id"": ""f3"",
            ""criteria"": { ""negatedQuery"": ""urgent"" },
            ""action"": { ""addLabelIds"": [""Label_Low""] }
        }";

        var filter = ImportFilterFromJson(json);

        var notCond = Assert.IsType<NotCondition>(filter.Query);
        var match = Assert.IsType<ExactMatchCondition>(notCond.InnerCondition);
        Assert.Equal("urgent", match.Phrase);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("Not(match(\"urgent\"))", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        Assert.Equal(filter.ToGmailQuery(), reloaded.ToGmailQuery());
    }

    [Fact]
    public async Task Import_NegatedQueryField_ProducesNotField()
    {
        string json = @"{
            ""id"": ""f4"",
            ""criteria"": { ""negatedQuery"": ""from:spam@evil.com"" },
            ""action"": { ""addLabelIds"": [""Label_Ok""] }
        }";

        var filter = ImportFilterFromJson(json);

        var notCond = Assert.IsType<NotCondition>(filter.Query);
        var innerField = Assert.IsType<FieldCondition>(notCond.InnerCondition);
        Assert.Equal("from", innerField.Field);
        Assert.Equal("spam@evil.com", innerField.Value);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("Not(From(\"spam@evil.com\"))", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        Assert.Equal(filter.ToGmailQuery(), reloaded.ToGmailQuery());
    }

    [Fact]
    public async Task Import_NegatedQueryAnd_ProducesNotAnd()
    {
        string json = @"{
            ""id"": ""f5"",
            ""criteria"": { ""negatedQuery"": ""from:spam.com to:finance@corp.com"" },
            ""action"": { ""addLabelIds"": [""Label_1""] }
        }";

        var filter = ImportFilterFromJson(json);

        var notCond = Assert.IsType<NotCondition>(filter.Query);
        var andCond = Assert.IsType<AndCondition>(notCond.InnerCondition);
        Assert.Equal(2, andCond.Conditions.Count);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("Not(And(", lua);
        Assert.Contains("From(\"spam.com\")", lua);
        Assert.Contains("To(\"finance@corp.com\")", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        Assert.Equal(filter.ToGmailQuery(), reloaded.ToGmailQuery());
    }

    [Fact]
    public async Task Import_NegatedQueryOr_ProducesNotOr()
    {
        string json = @"{
            ""id"": ""f6"",
            ""criteria"": { ""negatedQuery"": ""from:spam1.com OR from:spam2.com"" },
            ""action"": { ""addLabelIds"": [""Label_1""] }
        }";

        var filter = ImportFilterFromJson(json);

        var notCond = Assert.IsType<NotCondition>(filter.Query);
        var orCond = Assert.IsType<OrCondition>(notCond.InnerCondition);
        Assert.Equal(2, orCond.Conditions.Count);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("Not(Or(", lua);
        Assert.Contains("From(\"spam1.com\")", lua);
        Assert.Contains("From(\"spam2.com\")", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        Assert.Equal(filter.ToGmailQuery(), reloaded.ToGmailQuery());
    }

    [Fact]
    public async Task Import_QueryWithNegatedGroupInQueryField_ProducesNotOr()
    {
        string json = @"{
            ""id"": ""f7"",
            ""criteria"": { ""query"": ""-(from:spam1.com OR from:spam2.com)"" },
            ""action"": { ""addLabelIds"": [""Label_1""] }
        }";

        var filter = ImportFilterFromJson(json);

        var notCond = Assert.IsType<NotCondition>(filter.Query);
        var orCond = Assert.IsType<OrCondition>(notCond.InnerCondition);
        Assert.Equal(2, orCond.Conditions.Count);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("Not(Or(", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        Assert.Equal(filter.ToGmailQuery(), reloaded.ToGmailQuery());
    }

    // =========================================================================
    // 3. Or() with simplification (fields and not(fields) only)
    // =========================================================================

    [Fact]
    public async Task Import_OrQueryWithFields_ProducesOrCondition()
    {
        string json = @"{
            ""id"": ""f8"",
            ""criteria"": { ""query"": ""from:alice@example.com OR from:bob@example.com"" },
            ""action"": { ""addLabelIds"": [""Label_Team""] }
        }";

        var filter = ImportFilterFromJson(json);

        var orCond = Assert.IsType<OrCondition>(filter.Query);
        Assert.Equal(2, orCond.Conditions.Count);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("Or(", lua);
        Assert.Contains("From(\"alice@example.com\")", lua);
        Assert.Contains("From(\"bob@example.com\")", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        Assert.Equal(filter.ToGmailQuery(), reloaded.ToGmailQuery());
    }

    [Fact]
    public async Task Import_OrQueryWithFieldAndNotField_ProducesOrCondition()
    {
        string json = @"{
            ""id"": ""f9"",
            ""criteria"": { ""query"": ""from:alice@example.com OR -to:bob@example.com"" },
            ""action"": { ""addLabelIds"": [""Label_1""] }
        }";

        var filter = ImportFilterFromJson(json);

        var orCond = Assert.IsType<OrCondition>(filter.Query);
        Assert.Equal(2, orCond.Conditions.Count);
        Assert.Contains(orCond.Conditions, c => c is FieldCondition);
        Assert.Contains(orCond.Conditions, c => c is NotCondition);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("Or(", lua);
        Assert.Contains("Not(To(\"bob@example.com\"))", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        Assert.Equal(filter.ToGmailQuery(), reloaded.ToGmailQuery());
    }

    [Fact]
    public void Import_OrQueryContainingAnd_FallsBackToRawQuery()
    {
        // (from:a to:b) OR (from:c to:d) is not allowed by simplification rule: Or can only contain fields or not(fields)
        string json = @"{
            ""id"": ""f10"",
            ""criteria"": { ""query"": ""(from:a to:b) OR (from:c to:d)"" },
            ""action"": { ""addLabelIds"": [""Label_1""] }
        }";

        var filter = ImportFilterFromJson(json);

        var raw = Assert.IsType<RawQueryCondition>(filter.Query);
        Assert.Equal("(from:a to:b) OR (from:c to:d)", raw.Query);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("raw_query(\"(from:a to:b) OR (from:c to:d)\")", lua);
    }

    // =========================================================================
    // 4. And() containing fields, not(fields), and or()
    // =========================================================================

    [Fact]
    public async Task Import_AndWithFieldNotFieldAndOr_ProducesValidAndCondition()
    {
        string json = @"{
            ""id"": ""f11"",
            ""criteria"": {
                ""from"": ""boss@work.com"",
                ""query"": ""-is:starred (urgent OR priority)""
            },
            ""action"": { ""addLabelIds"": [""Label_Priority""] }
        }";

        var filter = ImportFilterFromJson(json);

        var andCond = Assert.IsType<AndCondition>(filter.Query);
        Assert.Equal(3, andCond.Conditions.Count);
        Assert.Contains(andCond.Conditions, c => c is FieldCondition);
        Assert.Contains(andCond.Conditions, c => c is NotCondition);
        Assert.Contains(andCond.Conditions, c => c is OrCondition);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("And(", lua);
        Assert.Contains("From(\"boss@work.com\")", lua);
        Assert.Contains("Not(is_starred)", lua);
        Assert.Contains("Or(", lua);
        Assert.Contains("match(\"priority\")", lua);
        Assert.Contains("match(\"urgent\")", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        Assert.Equal(filter.ToGmailQuery(), reloaded.ToGmailQuery());
    }

    [Fact]
    public void Import_AndContainingNotOr_FallsBackToRawQuery()
    {
        // An And containing Not(Or) violates the rule: And can only contain fields, not(fields), and or()
        string json = @"{
            ""id"": ""f12"",
            ""criteria"": {
                ""from"": ""team@work.com"",
                ""negatedQuery"": ""spam1.com OR spam2.com""
            },
            ""action"": { ""addLabelIds"": [""Label_1""] }
        }";

        var filter = ImportFilterFromJson(json);

        var raw = Assert.IsType<RawQueryCondition>(filter.Query);
        Assert.Contains("raw_query", _serializer.SerializeFilter(filter));
    }

    // =========================================================================
    // 5. Fallback Cases to raw_query()
    // =========================================================================

    [Fact]
    public void Import_MalformedQuery_FallsBackToRawQuery()
    {
        string json = @"{
            ""id"": ""f13"",
            ""criteria"": { ""query"": ""from:(unclosed parenthesis"" },
            ""action"": { ""addLabelIds"": [""Label_1""] }
        }";

        var filter = ImportFilterFromJson(json);

        var raw = Assert.IsType<RawQueryCondition>(filter.Query);
        Assert.Equal("from:(unclosed parenthesis", raw.Query);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("raw_query(\"from:(unclosed parenthesis\")", lua);
    }

    [Fact]
    public void Import_DeeplyNestedQuery_FallsBackToRawQuery()
    {
        string json = @"{
            ""id"": ""f14"",
            ""criteria"": { ""query"": ""from:a (b AND (c OR (d AND e)))"" },
            ""action"": { ""addLabelIds"": [""Label_1""] }
        }";

        var filter = ImportFilterFromJson(json);

        var raw = Assert.IsType<RawQueryCondition>(filter.Query);
        Assert.Equal("from:a (b AND (c OR (d AND e)))", raw.Query);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("raw_query", lua);
    }

    // =========================================================================
    // 6. FromDictionary Compatibility
    // =========================================================================

    [Fact]
    public void Import_FromDictionary_AppliesSameParsingRules()
    {
        var dict = new Dictionary<string, object?>
        {
            ["id"] = "f15",
            ["criteria"] = new Dictionary<string, object?>
            {
                ["from"] = "alerts@bank.com",
                ["subject"] = "Wire Transfer",
                ["hasAttachment"] = true
            },
            ["action"] = new Dictionary<string, object?>
            {
                ["addLabelIds"] = new[] { "Label_Finance" }
            }
        };

        var filter = GmailFilter.FromDictionary(dict);

        var and = Assert.IsType<AndCondition>(filter.Query);
        Assert.Equal(3, and.Conditions.Count);

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("From(\"alerts@bank.com\")", lua);
        Assert.Contains("Subject(\"Wire Transfer\")", lua);
        Assert.Contains("has_attachment", lua);
        Assert.DoesNotContain("raw_query", lua);
    }
}
