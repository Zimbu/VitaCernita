using System;
using System.Collections.Generic;
using Lua;
using VitaCernita.Core.Configuration;
using VitaCernita.Core.Filters.Validation;

namespace VitaCernita.Core.Filters;

public static class GmailFilterParser
{
    private static readonly string[] SupportedFieldNames =
    [
        "from", "to", "cc", "bcc", "subject", "list", "filename",
        "deliveredto", "delivered-to", "delivered_to", "rfc822msgid", "msgid",
        "header", "label",
        "after", "before", "older", "newer",
        "older_than", "newer_than", "older-than", "newer-than"
    ];

    public static GmailRule ParseRule(LuaTable table, string? customDateFormat = null)
    {
        string? ruleName = null;
        if (table.TryGetValue("name", out var nameVal) && nameVal.Type == LuaValueType.String)
        {
            ruleName = nameVal.Read<string>();
        }

        // Check if wrapped in `rule = ...`
        if (table.TryGetValue("rule", out var innerRule) && innerRule.TryRead<LuaTable>(out var innerTable))
        {
            return ParseRule(innerTable, customDateFormat);
        }

        // Check if wrapped in `match = ...` where match is a table (composite condition)
        if (table.TryGetValue("match", out var matchVal) && matchVal.TryRead<LuaTable>(out var matchTable))
        {
            var cond = ParseCondition(matchTable, customDateFormat);
            return new GmailRule(cond, ruleName);
        }

        var condition = ParseCondition(table, customDateFormat);
        return new GmailRule(condition, ruleName);
    }

    public static IFilterCondition ParseCondition(LuaTable table, string? customDateFormat = null)
    {
        // 1. Fluent Builder pattern: { type = "builder", conditions = { ... } }
        if (table.TryGetValue("type", out var typeVal) && typeVal.Type == LuaValueType.String &&
            typeVal.Read<string>() == "builder" &&
            table.TryGetValue("conditions", out var builderConds) && builderConds.TryRead<LuaTable>(out var bCondsTable))
        {
            return ParseConditionsList(bCondsTable, isOr: false, customDateFormat);
        }

        // 2. Exact word / phrase match node: { type = "exact", value = "..." }
        if (table.TryGetValue("type", out var exactTypeVal) && exactTypeVal.Type == LuaValueType.String &&
            exactTypeVal.Read<string>() == "exact")
        {
            string phrase = table.TryGetValue("value", out var pVal) ? pVal.ToString() : string.Empty;
            return new ExactMatchCondition(phrase);
        }

        // 3. Explicit DSL Operator: { type = "operator", op = "and"|"or", conditions = { ... } }
        if (table.TryGetValue("op", out var opVal) && opVal.Type == LuaValueType.String)
        {
            string op = opVal.Read<string>();
            bool isOr = op.Equals("or", StringComparison.OrdinalIgnoreCase);
            bool isAnd = op.Equals("and", StringComparison.OrdinalIgnoreCase);

            if (isOr || isAnd)
            {
                if (table.TryGetValue("conditions", out var condsVal) && condsVal.TryRead<LuaTable>(out var cTable))
                {
                    return ParseConditionsList(cTable, isOr, customDateFormat);
                }
                var directConds = ExtractDirectConditions(table, customDateFormat);
                if (directConds.Count > 0)
                {
                    if (directConds.Count == 1) return directConds[0];
                    return isOr ? new OrCondition(directConds) : new AndCondition(directConds);
                }
            }
        }

        // 4. Explicit DSL Field: { type = "field", field = "...", value = "..." }
        if (table.TryGetValue("type", out var fTypeVal) && fTypeVal.Type == LuaValueType.String &&
            fTypeVal.Read<string>() == "field")
        {
            string field = table.TryGetValue("field", out var fVal) ? fVal.ToString() : string.Empty;
            string val = table.TryGetValue("value", out var vVal) ? vVal.ToString() : string.Empty;
            return CreateConditionForField(field, val, customDateFormat);
        }

        // 5. Keyed operators: ["or"], ["any_of"], ["any"]
        foreach (string orKey in new[] { "or", "any_of", "any" })
        {
            if (table.TryGetValue(orKey, out var orVal) && orVal.TryRead<LuaTable>(out var orTable))
            {
                var orCond = ParseOperatorBlock(orTable, isOr: true, customDateFormat);
                var parentDirect = ExtractDirectConditions(table, customDateFormat);
                if (parentDirect.Count > 0)
                {
                    parentDirect.Add(orCond);
                    return new AndCondition(parentDirect);
                }
                return orCond;
            }
        }

        // 6. Keyed operators: ["and"], ["all_of"], ["all"]
        foreach (string andKey in new[] { "and", "all_of", "all" })
        {
            if (table.TryGetValue(andKey, out var andVal) && andVal.TryRead<LuaTable>(out var andTable))
            {
                var andCond = ParseOperatorBlock(andTable, isOr: false, customDateFormat);
                var parentDirect = ExtractDirectConditions(table, customDateFormat);
                if (parentDirect.Count > 0)
                {
                    parentDirect.Add(andCond);
                    return new AndCondition(parentDirect);
                }
                return andCond;
            }
        }

        // 7. Direct fields or string matches on the table
        var direct = ExtractDirectConditions(table, customDateFormat);
        if (direct.Count > 0)
        {
            return direct.Count == 1 ? direct[0] : new AndCondition(direct);
        }

        // 8. Array of conditions: { condition1, condition2 }
        if (table.ArrayLength > 0)
        {
            return ParseConditionsList(table, isOr: false, customDateFormat);
        }

        throw new LuaConfigException("Unable to parse Gmail filter condition from Lua table: no recognized fields or operators found.");
    }

    private static IFilterCondition ParseOperatorBlock(LuaTable blockTable, bool isOr, string? customDateFormat)
    {
        var conditions = new List<IFilterCondition>();

        // Extract direct field conditions or exact string matches
        var direct = ExtractDirectConditions(blockTable, customDateFormat);
        conditions.AddRange(direct);

        // Check if blockTable has array entries: { { from = "..." }, { to = "..." } }
        for (int i = 1; i <= blockTable.ArrayLength; i++)
        {
            var item = blockTable[i];
            if (item.TryRead<LuaTable>(out var childTable))
            {
                conditions.Add(ParseCondition(childTable, customDateFormat));
            }
        }

        // Also check any named keys that are tables (e.g. nested ["and"] = { ... } inside ["or"])
        foreach (var pair in blockTable)
        {
            if (pair.Key.Type != LuaValueType.Number && pair.Value.TryRead<LuaTable>(out var childTable))
            {
                string keyStr = pair.Key.ToString();
                if (!IsDirectField(keyStr))
                {
                    conditions.Add(ParseCondition(childTable, customDateFormat));
                }
            }
        }

        if (conditions.Count == 0)
        {
            throw new LuaConfigException($"Empty '{(isOr ? "or" : "and")}' condition block in Lua configuration.");
        }

        if (conditions.Count == 1) return conditions[0];
        return isOr ? new OrCondition(conditions) : new AndCondition(conditions);
    }

    private static IFilterCondition ParseConditionsList(LuaTable listTable, bool isOr, string? customDateFormat)
    {
        var conditions = new List<IFilterCondition>();

        for (int i = 1; i <= listTable.ArrayLength; i++)
        {
            var item = listTable[i];
            if (item.TryRead<LuaTable>(out var childTable))
            {
                conditions.Add(ParseCondition(childTable, customDateFormat));
            }
        }

        foreach (var pair in listTable)
        {
            if (pair.Key.Type != LuaValueType.Number && pair.Value.TryRead<LuaTable>(out var childTable))
            {
                conditions.Add(ParseCondition(childTable, customDateFormat));
            }
        }

        if (conditions.Count == 0)
        {
            throw new LuaConfigException("Condition list cannot be empty.");
        }

        if (conditions.Count == 1) return conditions[0];
        return isOr ? new OrCondition(conditions) : new AndCondition(conditions);
    }

    private static List<IFilterCondition> ExtractDirectConditions(LuaTable table, string? customDateFormat)
    {
        var conditions = new List<IFilterCondition>();

        // Check for exact phrase match via `match = "exact phrase"`
        if (table.TryGetValue("match", out var matchVal) && matchVal.Type == LuaValueType.String)
        {
            conditions.Add(new ExactMatchCondition(matchVal.Read<string>()));
        }

        // Check for field matches
        foreach (var field in SupportedFieldNames)
        {
            if (table.TryGetValue(field, out var val))
            {
                if (val.Type == LuaValueType.String)
                {
                    conditions.Add(CreateConditionForField(field, val.Read<string>(), customDateFormat));
                }
                else if (val.Type == LuaValueType.Table && field == "header" && val.TryRead<LuaTable>(out var hTable))
                {
                    string name = hTable.TryGetValue("name", out var n) ? n.ToString() : string.Empty;
                    string v = hTable.TryGetValue("value", out var vVal) ? vVal.ToString() : string.Empty;
                    conditions.Add(new FieldCondition("header", $"{name}:{v}"));
                }
            }
        }

        return conditions;
    }

    private static IFilterCondition CreateConditionForField(string field, string value, string? customDateFormat)
    {
        string normField = field.Trim().ToLowerInvariant();

        if (normField is "after" or "before" or "older" or "newer")
        {
            DateTime dt = FilterValidator.ValidateAndParseDate(normField, value, customDateFormat);
            return new DateCondition(normField, dt);
        }

        if (normField is "older_than" or "newer_than" or "older-than" or "newer-than")
        {
            string op = normField.Replace("-", "_");
            string dur = FilterValidator.ValidateAndNormalizeDuration(op, value);
            return new DurationCondition(op, dur);
        }

        return new FieldCondition(normField, value);
    }

    private static bool IsDirectField(string key)
    {
        if (key.Equals("match", StringComparison.OrdinalIgnoreCase)) return true;
        foreach (var field in SupportedFieldNames)
        {
            if (field.Equals(key, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
