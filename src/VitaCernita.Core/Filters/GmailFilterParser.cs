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
        "header", "label", "has", "is",
        "is_starred", "starred",
        "after", "before", "older", "newer",
        "older_than", "newer_than", "older-than", "newer-than",
        "has_yellow_star", "yellow_star", "yellow-star",
        "has_orange_star", "orange_star", "orange-star",
        "has_red_star", "red_star", "red-star",
        "has_purple_star", "purple_star", "purple-star",
        "has_blue_star", "blue_star", "blue-star",
        "has_green_star", "green_star", "green-star",
        "has_red_bang", "red_bang", "red-bang",
        "has_yellow_bang", "yellow_bang", "yellow-bang",
        "has_orange_guillemet", "orange_guillemet", "orange-guillemet", "orange_guillemets", "orange-guillemets",
        "has_green_check", "green_check", "green-check",
        "has_blue_info", "blue_info", "blue-info",
        "has_purple_question", "purple_question", "purple-question",
        "has_attachment", "attachment",
        "has_drive", "drive",
        "has_document", "document",
        "has_spreadsheet", "spreadsheet",
        "has_presentation", "presentation",
        "has_youtube", "has_you_tube", "youtube", "you_tube",
        "has_nouserlabels", "has_no_user_labels", "nouserlabels", "no_user_labels",
        "is_unread", "unread",
        "is_read", "read",
        "is_important", "important",
        "is_muted", "muted",
        "is_snoozed", "snoozed",
        "is_chat", "chat",
        "is_draft", "draft",
        "is_sent", "sent",
        "is_trash", "trash",
        "is_spam", "spam",
        "in",
        "in_anywhere", "anywhere",
        "in_archive", "archive",
        "in_snoozed",
        "in_inbox", "inbox",
        "in_sent",
        "in_drafts", "drafts",
        "in_trash",
        "in_spam",
        "in_chats", "chats",
        "category",
        "category_primary", "category-primary",
        "category_social", "category-social",
        "category_promotions", "category-promotions", "category_promotion", "category-promotion",
        "category_updates", "category-updates", "category_update", "category-update",
        "category_forums", "category-forums", "category_forum", "category-forum",
        "category_reservations", "category-reservations", "category_reservation", "category-reservation",
        "category_purchases", "category-purchases", "category_purchase", "category-purchase",
        "size", "larger", "smaller", "larger_than", "smaller_than", "larger-than", "smaller-than"
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

        // 3. Explicit DSL Operator: { type = "operator", op = "and"|"or"|"not", conditions/condition = ... }
        if (table.TryGetValue("op", out var opVal) && opVal.Type == LuaValueType.String)
        {
            string op = opVal.Read<string>();
            bool isOr = op.Equals("or", StringComparison.OrdinalIgnoreCase);
            bool isAnd = op.Equals("and", StringComparison.OrdinalIgnoreCase);
            bool isNot = op.Equals("not", StringComparison.OrdinalIgnoreCase);

            if (isNot)
            {
                if (!table.TryGetValue("condition", out var condVal) || condVal.Type == LuaValueType.Nil)
                {
                    throw new FilterValidationException("A 'not' condition cannot be empty; it must negate a valid expression.");
                }

                if (condVal.Type == LuaValueType.String)
                {
                    string strVal = condVal.Read<string>();
                    if (string.IsNullOrWhiteSpace(strVal))
                    {
                        throw new FilterValidationException("A 'not' condition cannot be empty; it must negate a valid expression.");
                    }
                    return new NotCondition(new ExactMatchCondition(strVal));
                }

                if (condVal.TryRead<LuaTable>(out var condTable))
                {
                    if (condTable.ArrayLength == 0 && !HasAnyNonEmptyKey(condTable))
                    {
                        throw new FilterValidationException("A 'not' condition cannot be empty; it must negate a valid expression.");
                    }
                    var inner = ParseCondition(condTable, customDateFormat);
                    return new NotCondition(inner);
                }

                throw new FilterValidationException("A 'not' condition cannot be empty; it must negate a valid expression.");
            }

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

        // 5. Explicit DSL Has / Star: { type = "has", value = "..." }
        if (table.TryGetValue("type", out var hTypeVal) && hTypeVal.Type == LuaValueType.String &&
            hTypeVal.Read<string>() == "has")
        {
            string val = table.TryGetValue("value", out var vVal) ? vVal.ToString() : string.Empty;
            return new HasCondition(val);
        }

        // 6. Explicit DSL Is: { type = "is", value = "..." }
        if (table.TryGetValue("type", out var isTypeVal) && isTypeVal.Type == LuaValueType.String &&
            isTypeVal.Read<string>() == "is")
        {
            string val = table.TryGetValue("value", out var vVal) ? vVal.ToString() : string.Empty;
            return new IsCondition(val);
        }

        // 7. Explicit DSL In: { type = "in", value = "..." }
        if (table.TryGetValue("type", out var inTypeVal) && inTypeVal.Type == LuaValueType.String &&
            inTypeVal.Read<string>() == "in")
        {
            string val = table.TryGetValue("value", out var vVal) ? vVal.ToString() : string.Empty;
            return new InCondition(val);
        }

        // 8. Explicit DSL Category: { type = "category", value = "..." }
        if (table.TryGetValue("type", out var catTypeVal) && catTypeVal.Type == LuaValueType.String &&
            catTypeVal.Read<string>() == "category")
        {
            string val = table.TryGetValue("value", out var vVal) ? vVal.ToString() : string.Empty;
            return new CategoryCondition(val);
        }

        // 9. Explicit DSL Size: { type = "size", ... }
        if (table.TryGetValue("type", out var sizeTypeVal) && sizeTypeVal.Type == LuaValueType.String &&
            sizeTypeVal.Read<string>() == "size")
        {
            string op = table.TryGetValue("op", out var sizeOpVal) ? sizeOpVal.ToString() : "size";
            string val = table.TryGetValue("value", out var vVal) ? vVal.ToString() : string.Empty;
            return new SizeCondition(op, val);
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
                else if (val.Type == LuaValueType.Boolean && val.Read<bool>() && IsHasKey(field))
                {
                    conditions.Add(new HasCondition(ExtractHasTargetFromKey(field)));
                }
                else if (val.Type == LuaValueType.Boolean && val.Read<bool>() && IsIsKey(field))
                {
                    conditions.Add(new IsCondition(ExtractIsTargetFromKey(field)));
                }
                else if (val.Type == LuaValueType.Table && field == "has" && val.TryRead<LuaTable>(out var hasTable))
                {
                    bool hasDirectStrings = false;
                    for (int i = 1; i <= hasTable.ArrayLength; i++)
                    {
                        if (hasTable[i].Type == LuaValueType.String)
                        {
                            conditions.Add(new HasCondition(hasTable[i].Read<string>()));
                            hasDirectStrings = true;
                        }
                    }
                    if (!hasDirectStrings)
                    {
                        conditions.Add(ParseCondition(hasTable, customDateFormat));
                    }
                }
                else if (val.Type == LuaValueType.Table && field == "is" && val.TryRead<LuaTable>(out var isTable))
                {
                    bool hasDirectStrings = false;
                    for (int i = 1; i <= isTable.ArrayLength; i++)
                    {
                        if (isTable[i].Type == LuaValueType.String)
                        {
                            conditions.Add(new IsCondition(isTable[i].Read<string>()));
                            hasDirectStrings = true;
                        }
                    }
                    if (!hasDirectStrings)
                    {
                        conditions.Add(ParseCondition(isTable, customDateFormat));
                    }
                }
                else if (val.Type == LuaValueType.Boolean && val.Read<bool>() && IsInKey(field))
                {
                    conditions.Add(new InCondition(ExtractInTargetFromKey(field)));
                }
                else if (val.Type == LuaValueType.Table && field == "in" && val.TryRead<LuaTable>(out var inTable))
                {
                    bool hasDirectStrings = false;
                    for (int i = 1; i <= inTable.ArrayLength; i++)
                    {
                        if (inTable[i].Type == LuaValueType.String)
                        {
                            conditions.Add(new InCondition(inTable[i].Read<string>()));
                            hasDirectStrings = true;
                        }
                    }
                    if (!hasDirectStrings)
                    {
                        conditions.Add(ParseCondition(inTable, customDateFormat));
                    }
                }
                else if (val.Type == LuaValueType.Boolean && val.Read<bool>() && IsCategoryKey(field))
                {
                    conditions.Add(new CategoryCondition(ExtractCategoryTargetFromKey(field)));
                }
                else if (val.Type == LuaValueType.Table && field == "category" && val.TryRead<LuaTable>(out var catTable))
                {
                    bool hasDirectStrings = false;
                    for (int i = 1; i <= catTable.ArrayLength; i++)
                    {
                        if (catTable[i].Type == LuaValueType.String)
                        {
                            conditions.Add(new CategoryCondition(catTable[i].Read<string>()));
                            hasDirectStrings = true;
                        }
                    }
                    if (!hasDirectStrings)
                    {
                        conditions.Add(ParseCondition(catTable, customDateFormat));
                    }
                }
                else if (val.Type == LuaValueType.Number && IsSizeField(field))
                {
                    long num = (long)val.Read<double>();
                    conditions.Add(new SizeCondition(field, num.ToString()));
                }
                else if (val.Type == LuaValueType.Table && IsSizeField(field) && val.TryRead<LuaTable>(out var sTable))
                {
                    bool hasDirect = false;
                    for (int i = 1; i <= sTable.ArrayLength; i++)
                    {
                        if (sTable[i].Type == LuaValueType.String)
                        {
                            conditions.Add(new SizeCondition(field, sTable[i].Read<string>()));
                            hasDirect = true;
                        }
                        else if (sTable[i].Type == LuaValueType.Number)
                        {
                            long num = (long)sTable[i].Read<double>();
                            conditions.Add(new SizeCondition(field, num.ToString()));
                            hasDirect = true;
                        }
                    }
                    if (!hasDirect)
                    {
                        conditions.Add(ParseCondition(sTable, customDateFormat));
                    }
                }
                else if (val.Type == LuaValueType.Table && field == "header" && val.TryRead<LuaTable>(out var hTable))
                {
                    string name = hTable.TryGetValue("name", out var n) ? n.ToString() : string.Empty;
                    string v = hTable.TryGetValue("value", out var vVal) ? vVal.ToString() : string.Empty;
                    conditions.Add(new FieldCondition("header", $"{name}:{v}"));
                }
            }
        }

        // Check for negation operator via ["not"], ["negate"], ["invert"]
        foreach (string notKey in new[] { "not", "negate", "invert" })
        {
            if (table.TryGetValue(notKey, out var notVal))
            {
                if (notVal.Type == LuaValueType.Nil)
                {
                    throw new FilterValidationException("A 'not' condition cannot be empty; it must negate a valid expression.");
                }

                if (notVal.Type == LuaValueType.String)
                {
                    string strVal = notVal.Read<string>();
                    if (string.IsNullOrWhiteSpace(strVal))
                    {
                        throw new FilterValidationException("A 'not' condition cannot be empty; it must negate a valid expression.");
                    }
                    conditions.Add(new NotCondition(new ExactMatchCondition(strVal)));
                }
                else if (notVal.TryRead<LuaTable>(out var notTable))
                {
                    if (notTable.ArrayLength == 0 && !HasAnyNonEmptyKey(notTable))
                    {
                        throw new FilterValidationException("A 'not' condition cannot be empty; it must negate a valid expression.");
                    }
                    var inner = ParseCondition(notTable, customDateFormat);
                    conditions.Add(new NotCondition(inner));
                }
                else
                {
                    throw new FilterValidationException("A 'not' condition cannot be empty; it must negate a valid expression.");
                }
            }
        }

        return conditions;
    }

    private static IFilterCondition CreateConditionForField(string field, string value, string? customDateFormat)
    {
        string normField = field.Trim().ToLowerInvariant();

        if (normField == "has")
        {
            return new HasCondition(value);
        }

        if (normField == "is")
        {
            return new IsCondition(value);
        }

        if (IsIsKey(normField))
        {
            return new IsCondition(ExtractIsTargetFromKey(normField));
        }

        if (normField == "in")
        {
            return new InCondition(value);
        }

        if (IsInKey(normField))
        {
            return new InCondition(ExtractInTargetFromKey(normField));
        }

        if (normField == "category")
        {
            return new CategoryCondition(value);
        }

        if (IsCategoryKey(normField))
        {
            return new CategoryCondition(ExtractCategoryTargetFromKey(normField));
        }

        if (IsSizeField(normField))
        {
            return new SizeCondition(normField, value);
        }

        if (IsHasKey(normField))
        {
            return new HasCondition(ExtractHasTargetFromKey(normField));
        }

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

    private static bool IsIsKey(string key)
    {
        string norm = key.Trim().ToLowerInvariant();
        if (norm.StartsWith("is_") || norm.StartsWith("is-"))
        {
            norm = norm[3..];
        }
        norm = norm.Replace('_', '-');
        return FilterValidator.CanonicalIsTargets.Contains(norm);
    }

    private static string ExtractIsTargetFromKey(string key)
    {
        string norm = key.Trim().ToLowerInvariant();
        if (norm.StartsWith("is_") || norm.StartsWith("is-"))
        {
            norm = norm[3..];
        }
        norm = norm.Replace('_', '-');
        return FilterValidator.ValidateAndNormalizeIsTarget("is", norm);
    }

    private static bool IsInKey(string key)
    {
        string norm = key.Trim().ToLowerInvariant();
        if (norm.StartsWith("in_") || norm.StartsWith("in-"))
        {
            norm = norm[3..];
        }
        norm = norm.Replace('_', '-');
        if (norm == "draft") norm = "drafts";
        if (norm == "chat") norm = "chats";
        return FilterValidator.CanonicalInTargets.Contains(norm);
    }

    private static string ExtractInTargetFromKey(string key)
    {
        string norm = key.Trim().ToLowerInvariant();
        if (norm.StartsWith("in_") || norm.StartsWith("in-"))
        {
            norm = norm[3..];
        }
        norm = norm.Replace('_', '-');
        return FilterValidator.ValidateAndNormalizeInTarget("in", norm);
    }

    private static bool IsCategoryKey(string key)
    {
        string norm = key.Trim().ToLowerInvariant();
        if (norm.StartsWith("category_") || norm.StartsWith("category-"))
        {
            norm = norm[9..];
        }
        norm = norm.Replace('_', '-');
        if (norm == "promotion") norm = "promotions";
        if (norm == "update") norm = "updates";
        if (norm == "forum") norm = "forums";
        if (norm == "reservation") norm = "reservations";
        if (norm == "purchase") norm = "purchases";
        return FilterValidator.CanonicalCategoryTargets.Contains(norm);
    }

    private static string ExtractCategoryTargetFromKey(string key)
    {
        string norm = key.Trim().ToLowerInvariant();
        if (norm.StartsWith("category_") || norm.StartsWith("category-"))
        {
            norm = norm[9..];
        }
        norm = norm.Replace('_', '-');
        return FilterValidator.ValidateAndNormalizeCategoryTarget("category", norm);
    }

    private static bool IsSizeField(string field)
    {
        string norm = field.Trim().ToLowerInvariant().Replace('-', '_');
        return norm is "size" or "larger" or "smaller" or "larger_than" or "smaller_than";
    }

    private static bool IsHasKey(string key)
    {
        string norm = key.Trim().ToLowerInvariant();
        if (norm.StartsWith("has_") || norm.StartsWith("has-"))
        {
            norm = norm[4..];
        }
        norm = norm.Replace('_', '-');
        if (norm.EndsWith("guillemets")) norm = norm[..^1];
        if (norm is "user-labels") norm = "userlabels";
        if (norm is "no-user-labels" or "no-userlabels" or "nouser-labels") norm = "nouserlabels";
        if (norm is "you-tube") norm = "youtube";
        return FilterValidator.CanonicalHasTargets.Contains(norm);
    }

    private static string ExtractHasTargetFromKey(string key)
    {
        string norm = key.Trim().ToLowerInvariant();
        if (norm.StartsWith("has_") || norm.StartsWith("has-"))
        {
            norm = norm[4..];
        }
        norm = norm.Replace('_', '-');
        if (norm.EndsWith("guillemets")) norm = norm[..^1];
        return FilterValidator.ValidateAndNormalizeHasTarget("has", norm);
    }

    private static bool HasAnyNonEmptyKey(LuaTable table)
    {
        foreach (var pair in table)
        {
            if (pair.Value.Type != LuaValueType.Nil)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsDirectField(string key)
    {
        if (key.Equals("match", StringComparison.OrdinalIgnoreCase)) return true;
        if (key.Equals("not", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("negate", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("invert", StringComparison.OrdinalIgnoreCase)) return true;
        foreach (var field in SupportedFieldNames)
        {
            if (field.Equals(key, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
