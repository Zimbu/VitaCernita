using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Lua;
using Lua.Standard;
using VitaCernita.Core.Configuration;

namespace VitaCernita.Core.Filters;

public sealed class GmailFilterLoader
{
    private const string DslPrelude = @"
-- =======================================================================
-- VitaCernita Gmail Filter DSL Prelude
-- =======================================================================

function from(val) return { type = 'field', field = 'from', value = tostring(val) } end
From = from

function to(val) return { type = 'field', field = 'to', value = tostring(val) } end
To = to

function cc(val) return { type = 'field', field = 'cc', value = tostring(val) } end
Cc = cc

function bcc(val) return { type = 'field', field = 'bcc', value = tostring(val) } end
Bcc = bcc

function subject(val) return { type = 'field', field = 'subject', value = tostring(val) } end
Subject = subject

function list(val) return { type = 'field', field = 'list', value = tostring(val) } end
List = list

function filename(val) return { type = 'field', field = 'filename', value = tostring(val) } end
Filename = filename

function delivered_to(val) return { type = 'field', field = 'deliveredto', value = tostring(val) } end
deliveredto = delivered_to
DeliveredTo = delivered_to

function rfc822msgid(val) return { type = 'field', field = 'rfc822msgid', value = tostring(val) } end
Rfc822MsgId = rfc822msgid
msgid = rfc822msgid

function header(name_or_pair, maybe_val)
    if maybe_val ~= nil then
        return { type = 'field', field = 'header', value = tostring(name_or_pair) .. ':' .. tostring(maybe_val) }
    else
        return { type = 'field', field = 'header', value = tostring(name_or_pair) }
    end
end
Header = header

function label(val) return { type = 'field', field = 'label', value = tostring(val) } end
Label = label

-- Exact word or phrase match: double-quoted search term
function match(phrase) return { type = 'exact', value = tostring(phrase) } end
Match = match
exact = match

-- Date operators
function after(val) return { type = 'field', field = 'after', value = tostring(val) } end
After = after

function before(val) return { type = 'field', field = 'before', value = tostring(val) } end
Before = before

function older(val) return { type = 'field', field = 'older', value = tostring(val) } end
Older = older

function newer(val) return { type = 'field', field = 'newer', value = tostring(val) } end
Newer = newer

-- Duration operators
function older_than(val) return { type = 'field', field = 'older_than', value = tostring(val) } end
OlderThan = older_than
olderThan = older_than

function newer_than(val) return { type = 'field', field = 'newer_than', value = tostring(val) } end
NewerThan = newer_than
newerThan = newer_than

-- Logical operators
function And(...)
    local args = { ... }
    return { type = 'operator', op = 'and', conditions = args }
end
all_of = And
All = And

function Or(...)
    local args = { ... }
    return { type = 'operator', op = 'or', conditions = args }
end
any_of = Or
Any = Or
either = Or

function rule(tbl)
    if type(tbl) == 'table' then
        return setmetatable({ type = 'rule', rule = tbl }, {
            __index = tbl
        })
    end
    return tbl
end
Rule = rule

local FilterBuilder = {}
FilterBuilder.__index = FilterBuilder

function FilterBuilder:from(val) table.insert(self.conditions, from(val)); return self end
FilterBuilder.From = FilterBuilder.from

function FilterBuilder:to(val) table.insert(self.conditions, to(val)); return self end
FilterBuilder.To = FilterBuilder.to

function FilterBuilder:cc(val) table.insert(self.conditions, cc(val)); return self end
FilterBuilder.Cc = FilterBuilder.cc

function FilterBuilder:bcc(val) table.insert(self.conditions, bcc(val)); return self end
FilterBuilder.Bcc = FilterBuilder.bcc

function FilterBuilder:subject(val) table.insert(self.conditions, subject(val)); return self end
FilterBuilder.Subject = FilterBuilder.subject

function FilterBuilder:list(val) table.insert(self.conditions, list(val)); return self end
FilterBuilder.List = FilterBuilder.list

function FilterBuilder:filename(val) table.insert(self.conditions, filename(val)); return self end
FilterBuilder.Filename = FilterBuilder.filename

function FilterBuilder:delivered_to(val) table.insert(self.conditions, delivered_to(val)); return self end
FilterBuilder.DeliveredTo = FilterBuilder.delivered_to

function FilterBuilder:rfc822msgid(val) table.insert(self.conditions, rfc822msgid(val)); return self end
FilterBuilder.Rfc822MsgId = FilterBuilder.rfc822msgid

function FilterBuilder:header(name_or_pair, maybe_val) table.insert(self.conditions, header(name_or_pair, maybe_val)); return self end
FilterBuilder.Header = FilterBuilder.header

function FilterBuilder:label(val) table.insert(self.conditions, label(val)); return self end
FilterBuilder.Label = FilterBuilder.label

function FilterBuilder:match(phrase) table.insert(self.conditions, match(phrase)); return self end
FilterBuilder.Match = FilterBuilder.match

function FilterBuilder:after(val) table.insert(self.conditions, after(val)); return self end
FilterBuilder.After = FilterBuilder.after

function FilterBuilder:before(val) table.insert(self.conditions, before(val)); return self end
FilterBuilder.Before = FilterBuilder.before

function FilterBuilder:older(val) table.insert(self.conditions, older(val)); return self end
FilterBuilder.Older = FilterBuilder.older

function FilterBuilder:newer(val) table.insert(self.conditions, newer(val)); return self end
FilterBuilder.Newer = FilterBuilder.newer

function FilterBuilder:older_than(val) table.insert(self.conditions, older_than(val)); return self end
FilterBuilder.OlderThan = FilterBuilder.older_than

function FilterBuilder:newer_than(val) table.insert(self.conditions, newer_than(val)); return self end
FilterBuilder.NewerThan = FilterBuilder.newer_than

function FilterBuilder:build()
    return { type = 'operator', op = 'and', conditions = self.conditions }
end

function filter()
    return setmetatable({ type = 'builder', conditions = {} }, FilterBuilder)
end
Filter = filter
";

    public async Task<List<GmailRule>> LoadRulesFromFileAsync(string filePath, LuaState? externalState = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Filter configuration file not found: {filePath}", filePath);
        }

        string script = await File.ReadAllTextAsync(filePath);
        return await LoadRulesFromScriptAsync(script, externalState);
    }

    public async Task<GmailRule> LoadRuleFromScriptAsync(string script, LuaState? externalState = null)
    {
        var rules = await LoadRulesFromScriptAsync(script, externalState);
        if (rules.Count == 0)
        {
            throw new LuaConfigException("No Gmail rules found in configuration script.");
        }
        return rules[0];
    }

    public async Task<List<GmailRule>> LoadRulesFromScriptAsync(string script, LuaState? externalState = null)
    {
        var state = externalState ?? LuaState.Create();
        state.OpenStandardLibraries();
        RegisterHelpers(state);

        await state.DoStringAsync(DslPrelude);

        LuaValue[] results;
        try
        {
            results = await state.DoStringAsync(script);
        }
        catch (Exception ex)
        {
            throw new LuaConfigException($"Failed to evaluate Lua filter script: {ex.Message}", ex);
        }

        LuaTable rootTable;
        if (results.Length > 0 && results[0].TryRead<LuaTable>(out var retTable))
        {
            rootTable = retTable;
        }
        else if (state.Environment.TryGetValue("config", out var gCfg) && gCfg.TryRead<LuaTable>(out var gTable))
        {
            rootTable = gTable;
        }
        else if (state.Environment.TryGetValue("rule", out var gRule) && gRule.TryRead<LuaTable>(out var rTable))
        {
            rootTable = rTable;
        }
        else
        {
            throw new LuaConfigException("Lua filter script must return a rule table or define a global 'rule' or 'config' table.");
        }

        string? customDateFormat = ExtractDateFormat(rootTable, state);

        return ParseRulesFromRoot(rootTable, customDateFormat);
    }

    private static string? ExtractDateFormat(LuaTable root, LuaState state)
    {
        if (root.TryGetValue("date_format", out var dfVal) && dfVal.Type == LuaValueType.String)
        {
            return dfVal.Read<string>();
        }
        if (root.TryGetValue("DateFormat", out var dfValUpper) && dfValUpper.Type == LuaValueType.String)
        {
            return dfValUpper.Read<string>();
        }
        if (root.TryGetValue("settings", out var sVal) && sVal.TryRead<LuaTable>(out var sTable) &&
            sTable.TryGetValue("date_format", out var sdfVal) && sdfVal.Type == LuaValueType.String)
        {
            return sdfVal.Read<string>();
        }
        if (state.Environment.TryGetValue("date_format", out var envDf) && envDf.Type == LuaValueType.String)
        {
            return envDf.Read<string>();
        }
        return null;
    }

    private static void RegisterHelpers(LuaState state)
    {
        state.Environment["env"] = new LuaFunction((context, ct) =>
        {
            var key = context.GetArgument<string>(0);
            var defaultVal = context.HasArgument(1) ? context.GetArgument<string>(1) : string.Empty;
            var val = Environment.GetEnvironmentVariable(key) ?? defaultVal;
            return ValueTask.FromResult(context.Return(val));
        });

        state.Environment["platform"] = new LuaFunction((context, ct) =>
        {
            string os = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux"
                      : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
                      : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
                      : "Unknown";
            return ValueTask.FromResult(context.Return(os));
        });
    }

    private static List<GmailRule> ParseRulesFromRoot(LuaTable root, string? customDateFormat)
    {
        var rules = new List<GmailRule>();

        // Check if root has a "rules" table: { rules = { ... } }
        if (root.TryGetValue("rules", out var rulesVal) && rulesVal.TryRead<LuaTable>(out var rulesTable))
        {
            for (int i = 1; i <= rulesTable.ArrayLength; i++)
            {
                if (rulesTable[i].TryRead<LuaTable>(out var rTable))
                {
                    rules.Add(GmailFilterParser.ParseRule(rTable, customDateFormat));
                }
            }
            foreach (var pair in rulesTable)
            {
                if (pair.Key.Type != LuaValueType.Number && pair.Value.TryRead<LuaTable>(out var rTable))
                {
                    rules.Add(GmailFilterParser.ParseRule(rTable, customDateFormat));
                }
            }
            if (rules.Count > 0) return rules;
        }

        // Check if root is an operator or exact match
        if (root.TryGetValue("type", out var typeVal))
        {
            string t = typeVal.ToString();
            if (t == "operator" || t == "exact" || t == "field" || t == "builder")
            {
                rules.Add(GmailFilterParser.ParseRule(root, customDateFormat));
                return rules;
            }
        }

        // Check if root is an array of rules: { rule1, rule2 }
        if (root.ArrayLength > 0 && root[1].Type == LuaValueType.Table)
        {
            for (int i = 1; i <= root.ArrayLength; i++)
            {
                if (root[i].TryRead<LuaTable>(out var rTable))
                {
                    rules.Add(GmailFilterParser.ParseRule(rTable, customDateFormat));
                }
            }
            if (rules.Count > 0) return rules;
        }

        // Single rule
        rules.Add(GmailFilterParser.ParseRule(root, customDateFormat));
        return rules;
    }
}
