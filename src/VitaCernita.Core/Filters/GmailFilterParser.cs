using System;
using Lua;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Queries;

namespace VitaCernita.Core.Filters;

public static class GmailFilterParser
{
    public static GmailFilter ParseFilter(LuaTable table, string? customDateFormat = null)
    {
        string? filterId = null;
        if (table.TryGetValue("id", out var idVal) && idVal.Type != LuaValueType.Nil)
        {
            filterId = idVal.ToString();
        }
        else if (table.TryGetValue("filter_id", out var fidVal) && fidVal.Type != LuaValueType.Nil)
        {
            filterId = fidVal.ToString();
        }

        string? filterName = null;
        if (table.TryGetValue("name", out var nameVal) && nameVal.Type == LuaValueType.String)
        {
            filterName = nameVal.Read<string>();
        }
        else if (table.TryGetValue("filter_name", out var fnameVal) && fnameVal.Type == LuaValueType.String)
        {
            filterName = fnameVal.Read<string>();
        }

        // Check if wrapped in `filter = ...` or `rule = ...` or `definition = ...`
        if (table.TryGetValue("filter", out var innerFilter) && innerFilter.TryRead<LuaTable>(out var filterTable))
        {
            var parsed = ParseFilter(filterTable, customDateFormat);
            parsed.Id ??= filterId;
            parsed.Name ??= filterName;
            return parsed;
        }
        if (table.TryGetValue("rule", out var innerRule) && innerRule.TryRead<LuaTable>(out var ruleTable))
        {
            var parsed = ParseFilter(ruleTable, customDateFormat);
            parsed.Id ??= filterId;
            parsed.Name ??= filterName;
            return parsed;
        }
        if (table.TryGetValue("definition", out var innerDef) && innerDef.TryRead<LuaTable>(out var defTable) &&
            table.TryGetValue("type", out var defTypeVal) && defTypeVal.ToString() is "filter" or "rule")
        {
            var parsed = ParseFilter(defTable, customDateFormat);
            parsed.Id ??= filterId;
            parsed.Name ??= filterName;
            return parsed;
        }

        // Check if this table itself is explicitly an action object
        if (table.TryGetValue("type", out var rootTypeVal))
        {
            string rt = rootTypeVal.ToString();
            if (rt is "action" or "action_builder" or "action_item")
            {
                var act = GmailActionParser.ParseAction(table);
                return new GmailFilter(id: filterId, query: null, action: act, name: filterName);
            }
        }

        // Extract action if defined
        GmailAction? action = null;
        if (table.TryGetValue("action", out var actVal))
        {
            if (actVal.TryRead<LuaTable>(out var actTable))
            {
                action = GmailActionParser.ParseAction(actTable);
            }
            else if (actVal.Type == LuaValueType.String)
            {
                action = GmailActionParser.ParseActionString(actVal.Read<string>());
            }
        }
        else if (table.TryGetValue("actions", out var actsVal) && actsVal.TryRead<LuaTable>(out var actsTable))
        {
            action = GmailActionParser.ParseAction(actsTable);
        }

        // Extract query condition if defined
        IQueryCondition? query = null;
        if (table.TryGetValue("query", out var queryVal))
        {
            if (queryVal.TryRead<LuaTable>(out var queryTable))
            {
                query = GmailQueryParser.ParseQuery(queryTable, customDateFormat);
            }
            else if (queryVal.Type == LuaValueType.String)
            {
                query = new ExactMatchCondition(queryVal.Read<string>());
            }
        }
        else if (table.TryGetValue("criteria", out var criteriaVal))
        {
            if (criteriaVal.TryRead<LuaTable>(out var criteriaTable))
            {
                if (criteriaTable.TryGetValue("query", out var subQueryVal))
                {
                    if (subQueryVal.TryRead<LuaTable>(out var subQueryTable))
                    {
                        query = GmailQueryParser.ParseQuery(subQueryTable, customDateFormat);
                    }
                    else if (subQueryVal.Type == LuaValueType.String)
                    {
                        query = new ExactMatchCondition(subQueryVal.Read<string>());
                    }
                }
                else
                {
                    query = GmailQueryParser.ParseQuery(criteriaTable, customDateFormat);
                }
            }
        }
        else if (table.TryGetValue("match", out var matchVal) && matchVal.TryRead<LuaTable>(out var matchTable))
        {
            query = GmailQueryParser.ParseQuery(matchTable, customDateFormat);
        }
        else if (table.TryGetValue("conditions", out var condsVal) && condsVal.TryRead<LuaTable>(out var condsTable))
        {
            query = GmailQueryParser.ParseCondition(table, customDateFormat);
        }
        else if (GmailQueryParser.HasAnyConditionFields(table))
        {
            query = GmailQueryParser.ParseCondition(table, customDateFormat);
        }

        return new GmailFilter(id: filterId, query: query, action: action, name: filterName);
    }

    public static GmailRule ParseRule(LuaTable table, string? customDateFormat = null)
    {
        var filter = ParseFilter(table, customDateFormat);
        return new GmailRule(condition: filter.Query, name: filter.Name, action: filter.Action, id: filter.Id);
    }

    public static IQueryCondition ParseCondition(LuaTable table, string? customDateFormat = null)
    {
        return GmailQueryParser.ParseCondition(table, customDateFormat);
    }
}
