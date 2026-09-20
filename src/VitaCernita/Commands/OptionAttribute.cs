using System;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// Marks a property as a CLI option, switch, or parameter on a command.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class OptionAttribute : Attribute
{
    /// <summary>
    /// Long name for the option without prefix (e.g. "config" for "--config").
    /// If null or whitespace, automatically inferred as the kebab-case of the property name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Single-character short alias without prefix (e.g. 'c' for "-c").
    /// '\0' indicates no short alias is defined.
    /// </summary>
    public char ShortName { get; init; } = '\0';

    /// <summary>
    /// Additional long aliases (e.g. ["dry-run"] for "--diff").
    /// </summary>
    public string[] Aliases { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Help description displayed in command usage.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Whether this option is required. Defaults to false.
    /// </summary>
    public bool Required { get; init; } = false;

    /// <summary>
    /// Value placeholder displayed in help (e.g. "&lt;path&gt;", "&lt;token&gt;").
    /// If omitted, automatically inferred from the option name.
    /// </summary>
    public string? ValueHelp { get; init; }

    public OptionAttribute()
    {
        Name = null;
    }

    public OptionAttribute(string name)
    {
        Name = name;
    }

    public OptionAttribute(char shortName)
    {
        ShortName = shortName;
    }

    public OptionAttribute(string name, char shortName)
    {
        Name = name;
        ShortName = shortName;
    }
}
