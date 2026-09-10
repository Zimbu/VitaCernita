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

function from(val)
    return { type = 'field', field = 'from', value = tostring(val) }
end
From = from

function subject(val)
    return { type = 'field', field = 'subject', value = tostring(val) }
end
Subject = subject

function And(...)
    local args = { ... }
    return { type = 'operator', op = 'and', conditions = args }
end
all_of = And
All = And

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

function FilterBuilder:from(val)
    table.insert(self.conditions, from(val))
    return self
end
FilterBuilder.From = FilterBuilder.from

function FilterBuilder:subject(val)
    table.insert(self.conditions, subject(val))
    return self
end
FilterBuilder.Subject = FilterBuilder.subject

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

        return ParseRulesFromRoot(rootTable);
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

    private static List<GmailRule> ParseRulesFromRoot(LuaTable root)
    {
        var rules = new List<GmailRule>();

        // Check if root has a "rules" table: { rules = { ... } }
        if (root.TryGetValue("rules", out var rulesVal) && rulesVal.TryRead<LuaTable>(out var rulesTable))
        {
            for (int i = 1; i <= rulesTable.ArrayLength; i++)
            {
                if (rulesTable[i].TryRead<LuaTable>(out var rTable))
                {
                    rules.Add(GmailFilterParser.ParseRule(rTable));
                }
            }
            foreach (var pair in rulesTable)
            {
                if (pair.Key.Type != LuaValueType.Number && pair.Value.TryRead<LuaTable>(out var rTable))
                {
                    rules.Add(GmailFilterParser.ParseRule(rTable));
                }
            }
            if (rules.Count > 0) return rules;
        }

        // Check if root is an array of rules: { rule1, rule2 }
        if (root.ArrayLength > 0 && root[1].Type == LuaValueType.Table)
        {
            // Check if root[1] looks like a rule or condition
            if (root.TryGetValue("type", out var typeVal) && typeVal.ToString() == "operator")
            {
                // This is an operator table, parse as single rule
                rules.Add(GmailFilterParser.ParseRule(root));
                return rules;
            }

            for (int i = 1; i <= root.ArrayLength; i++)
            {
                if (root[i].TryRead<LuaTable>(out var rTable))
                {
                    rules.Add(GmailFilterParser.ParseRule(rTable));
                }
            }
            if (rules.Count > 0) return rules;
        }

        // Single rule
        rules.Add(GmailFilterParser.ParseRule(root));
        return rules;
    }
}
