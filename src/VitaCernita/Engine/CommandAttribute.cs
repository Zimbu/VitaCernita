using System;

namespace VitaCernita.Cli.Engine;

/// <summary>
/// Defines metadata for a CLI command discovered at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    /// The primary name of the command. If null or whitespace, designates this command as the default command.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Brief description of the command for CLI help output.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Alternate names or abbreviations for this command.
    /// </summary>
    public string[] Aliases { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Indicates whether this command is the default command (has no primary name).
    /// </summary>
    public bool IsDefault => string.IsNullOrWhiteSpace(Name);

    public CommandAttribute()
    {
        Name = null;
    }

    public CommandAttribute(string name)
    {
        Name = name;
    }
}
