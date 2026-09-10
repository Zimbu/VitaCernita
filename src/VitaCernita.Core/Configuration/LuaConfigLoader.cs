using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Lua;

namespace VitaCernita.Core.Configuration;

/// <summary>
/// Loads and validates application configuration from Lua scripts or files.
/// </summary>
public sealed class LuaConfigLoader
{
    /// <summary>
    /// Loads configuration from a specified .lua file path.
    /// </summary>
    public async Task<AppConfig> LoadFromFileAsync(string filePath, LuaState? externalState = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Lua configuration file not found: {filePath}", filePath);
        }

        var script = await File.ReadAllTextAsync(filePath);
        return await LoadFromScriptAsync(script, externalState);
    }

    /// <summary>
    /// Loads configuration by evaluating a Lua script string.
    /// </summary>
    public async Task<AppConfig> LoadFromScriptAsync(string luaScript, LuaState? externalState = null)
    {
        var state = externalState ?? LuaState.Create();
        RegisterBuiltInFunctions(state);

        LuaValue[] results;
        try
        {
            results = await state.DoStringAsync(luaScript);
        }
        catch (Exception ex)
        {
            throw new LuaConfigException($"Failed to evaluate Lua configuration: {ex.Message}", ex);
        }

        LuaTable rootTable;
        if (results.Length > 0 && results[0].TryRead<LuaTable>(out var returnedTable))
        {
            rootTable = returnedTable;
        }
        else if (state.Environment.TryGetValue("config", out var globalConfig) && globalConfig.TryRead<LuaTable>(out var table))
        {
            rootTable = table;
        }
        else
        {
            throw new LuaConfigException("Lua configuration must return a table (e.g. `return { ... }`) or define a global `config = { ... }` table.");
        }

        return ParseConfigTable(rootTable);
    }

    private static void RegisterBuiltInFunctions(LuaState state)
    {
        // Helper: env("VAR_NAME", "default_value")
        state.Environment["env"] = new LuaFunction((context, ct) =>
        {
            var key = context.GetArgument<string>(0);
            var defaultVal = context.HasArgument(1) ? context.GetArgument<string>(1) : string.Empty;
            var val = Environment.GetEnvironmentVariable(key) ?? defaultVal;
            return ValueTask.FromResult(context.Return(val));
        });

        // Helper: platform() returns "Linux", "macOS", "Windows", etc.
        state.Environment["platform"] = new LuaFunction((context, ct) =>
        {
            string os = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux"
                      : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
                      : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
                      : "Unknown";
            return ValueTask.FromResult(context.Return(os));
        });
    }

    private static AppConfig ParseConfigTable(LuaTable table)
    {
        var config = new AppConfig { RawTable = table };

        // Parse project metadata
        if (table.TryGetValue("project", out var projVal) && projVal.TryRead<LuaTable>(out var projTable))
        {
            config.Project.Name = ReadString(projTable, "name", config.Project.Name);
            config.Project.Version = ReadString(projTable, "version", config.Project.Version);
            config.Project.Description = ReadString(projTable, "description", config.Project.Description);
            config.Project.Author = ReadString(projTable, "author", config.Project.Author);
        }

        // Parse runtime settings
        if (table.TryGetValue("settings", out var settingsVal) && settingsVal.TryRead<LuaTable>(out var settingsTable))
        {
            config.Settings.LogLevel = ReadString(settingsTable, "log_level", config.Settings.LogLevel);
            config.Settings.MaxConcurrency = ReadInt(settingsTable, "max_concurrency", config.Settings.MaxConcurrency);
            config.Settings.OutputDirectory = ReadString(settingsTable, "output_dir", config.Settings.OutputDirectory);
            config.Settings.DryRun = ReadBool(settingsTable, "dry_run", config.Settings.DryRun);
            config.Settings.TimeoutSeconds = ReadInt(settingsTable, "timeout_seconds", config.Settings.TimeoutSeconds);
        }

        // Parse rules
        if (table.TryGetValue("rules", out var rulesVal) && rulesVal.TryRead<LuaTable>(out var rulesTable))
        {
            foreach (var pair in rulesTable)
            {
                if (pair.Value.TryRead<LuaTable>(out var ruleTable))
                {
                    var rule = new TriageRule
                    {
                        Id = ReadString(ruleTable, "id", pair.Key.ToString()),
                        Description = ReadString(ruleTable, "description", string.Empty),
                        Priority = ReadInt(ruleTable, "priority", 100),
                        Enabled = ReadBool(ruleTable, "enabled", true),
                        Category = ReadString(ruleTable, "category", "Default")
                    };

                    if (ruleTable.TryGetValue("params", out var paramsVal) && paramsVal.TryRead<LuaTable>(out var paramsTable))
                    {
                        foreach (var paramPair in paramsTable)
                        {
                            rule.Parameters[paramPair.Key.ToString()] = paramPair.Value.ToString();
                        }
                    }

                    config.Rules.Add(rule);
                }
            }
        }

        // Parse custom properties
        if (table.TryGetValue("custom", out var customVal) && customVal.TryRead<LuaTable>(out var customTable))
        {
            foreach (var pair in customTable)
            {
                config.CustomProperties[pair.Key.ToString()] = pair.Value.ToString();
            }
        }

        return config;
    }

    private static string ReadString(LuaTable table, string key, string defaultValue)
    {
        if (table.TryGetValue(key, out var val) && val.Type == LuaValueType.String)
        {
            return val.Read<string>();
        }
        return defaultValue;
    }

    private static int ReadInt(LuaTable table, string key, int defaultValue)
    {
        if (table.TryGetValue(key, out var val) && val.Type == LuaValueType.Number)
        {
            return val.Read<int>();
        }
        return defaultValue;
    }

    private static bool ReadBool(LuaTable table, string key, bool defaultValue)
    {
        if (table.TryGetValue(key, out var val) && val.Type == LuaValueType.Boolean)
        {
            return val.Read<bool>();
        }
        return defaultValue;
    }
}
