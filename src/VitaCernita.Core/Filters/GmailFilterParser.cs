using System;
using System.Collections.Generic;
using Lua;
using VitaCernita.Core.Configuration;

namespace VitaCernita.Core.Filters;

public static class GmailFilterParser
{
    public static GmailRule ParseRule(LuaTable table)
    {
        string? ruleName = null;
        if (table.TryGetValue("name", out var nameVal) && nameVal.Type == LuaValueType.String)
        {
            ruleName = nameVal.Read<string>();
        }

        // Check if wrapped in `rule = ...` or `match = ...`
        if (table.TryGetValue("rule", out var innerRule) && innerRule.TryRead<LuaTable>(out var innerTable))
        {
            return ParseRule(innerTable);
        }
        if (table.TryGetValue("match", out var matchVal) && matchVal.TryRead<LuaTable>(out var matchTable))
        {
            var cond = ParseCondition(matchTable);
            return new GmailRule(cond, ruleName);
        }

        var condition = ParseCondition(table);
        return new GmailRule(condition, ruleName);
    }

    public static IFilterCondition ParseCondition(LuaTable table)
    {
        // 1. Fluent Builder pattern: { type = "builder", conditions = { ... } }
        if (table.TryGetValue("type", out var typeVal) && typeVal.Type == LuaValueType.String &&
            typeVal.Read<string>() == "builder" &&
            table.TryGetValue("conditions", out var builderConds) && builderConds.TryRead<LuaTable>(out var bCondsTable))
        {
            return ParseConditionsList(bCondsTable);
        }

        // 2. Explicit DSL Operator: { type = "operator", op = "and", conditions = { ... } }
        if (table.TryGetValue("op", out var opVal) && opVal.Type == LuaValueType.String &&
            opVal.Read<string>().Equals("and", StringComparison.OrdinalIgnoreCase))
        {
            if (table.TryGetValue("conditions", out var condsVal) && condsVal.TryRead<LuaTable>(out var cTable))
            {
                return ParseConditionsList(cTable);
            }
            // Or op = "and" with direct fields on the table
            var directConds = ExtractDirectFields(table);
            if (directConds.Count > 0)
            {
                return directConds.Count == 1 ? directConds[0] : new AndCondition(directConds);
            }
        }

        // 3. Explicit DSL Field: { type = "field", field = "from", value = "..." }
        if (table.TryGetValue("type", out var fTypeVal) && fTypeVal.Type == LuaValueType.String &&
            fTypeVal.Read<string>() == "field")
        {
            string field = table.TryGetValue("field", out var fVal) ? fVal.ToString() : string.Empty;
            string val = table.TryGetValue("value", out var vVal) ? vVal.ToString() : string.Empty;
            return new FieldCondition(field, val);
        }

        // 4. Keyed ["and"] / ["all_of"] / ["all"]:
        // e.g. { ["and"] = { from = "...", subject = "..." } }
        // or   { ["and"] = { { from = "..." }, { subject = "..." } } }
        foreach (string andKey in new[] { "and", "all_of", "all" })
        {
            if (table.TryGetValue(andKey, out var andVal) && andVal.TryRead<LuaTable>(out var andTable))
            {
                return ParseAndBlock(andTable);
            }
        }

        // 5. Direct fields on the table: e.g. { from = "...", subject = "..." }
        var direct = ExtractDirectFields(table);
        if (direct.Count > 0)
        {
            return direct.Count == 1 ? direct[0] : new AndCondition(direct);
        }

        // 6. Array of conditions: { condition1, condition2 }
        if (table.ArrayLength > 0)
        {
            return ParseConditionsList(table);
        }

        throw new LuaConfigException("Unable to parse Gmail filter condition from Lua table: no recognized fields or operators found.");
    }

    private static AndCondition ParseAndBlock(LuaTable andTable)
    {
        var conditions = new List<IFilterCondition>();

        // Check if andTable has direct field properties (from = "...", subject = "...")
        var direct = ExtractDirectFields(andTable);
        conditions.AddRange(direct);

        // Check if andTable has array entries: { { from = "..." }, { subject = "..." } }
        for (int i = 1; i <= andTable.ArrayLength; i++)
        {
            var item = andTable[i];
            if (item.TryRead<LuaTable>(out var childTable))
            {
                conditions.Add(ParseCondition(childTable));
            }
        }

        if (conditions.Count == 0)
        {
            throw new LuaConfigException("Empty 'and' condition block in Lua configuration.");
        }

        return new AndCondition(conditions);
    }

    private static AndCondition ParseConditionsList(LuaTable listTable)
    {
        var conditions = new List<IFilterCondition>();

        // Iterate through 1-indexed array elements
        for (int i = 1; i <= listTable.ArrayLength; i++)
        {
            var item = listTable[i];
            if (item.TryRead<LuaTable>(out var childTable))
            {
                conditions.Add(ParseCondition(childTable));
            }
        }

        // Also check if any key-value pairs exist
        foreach (var pair in listTable)
        {
            if (pair.Key.Type != LuaValueType.Number && pair.Value.TryRead<LuaTable>(out var childTable))
            {
                conditions.Add(ParseCondition(childTable));
            }
        }

        return new AndCondition(conditions);
    }

    private static List<IFilterCondition> ExtractDirectFields(LuaTable table)
    {
        var conditions = new List<IFilterCondition>();
        string[] supportedFields = ["from", "subject", "to", "cc", "bcc", "has"];

        foreach (var field in supportedFields)
        {
            if (table.TryGetValue(field, out var val) && val.Type == LuaValueType.String)
            {
                conditions.Add(new FieldCondition(field, val.Read<string>()));
            }
        }

        return conditions;
    }
}
