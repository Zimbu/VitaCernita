using System;
using System.Collections.Generic;
using Lua;

namespace VitaCernita.Core.Configuration;

/// <summary>
/// Root configuration parsed from the Lua configuration file.
/// </summary>
public sealed class AppConfig
{
    public ProjectMetadata Project { get; set; } = new();
    public RuntimeSettings Settings { get; set; } = new();
    public List<TriageRule> Rules { get; set; } = new();
    public Dictionary<string, string> CustomProperties { get; set; } = new();

    /// <summary>
    /// Underlying LuaTable returned by the script, preserved for dynamic Lua function calls.
    /// </summary>
    public LuaTable? RawTable { get; set; }
}

public sealed class ProjectMetadata
{
    public string Name { get; set; } = "VitaCernita";
    public string Version { get; set; } = "0.1.0";
    public string Description { get; set; } = "Cross-platform core with Lua configuration";
    public string Author { get; set; } = string.Empty;
}

public sealed class RuntimeSettings
{
    public string LogLevel { get; set; } = "Information";
    public int MaxConcurrency { get; set; } = 4;
    public string OutputDirectory { get; set; } = "./output";
    public bool DryRun { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class TriageRule
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; } = 100;
    public bool Enabled { get; set; } = true;
    public string Category { get; set; } = "Default";
    public Dictionary<string, string> Parameters { get; set; } = new();
}
