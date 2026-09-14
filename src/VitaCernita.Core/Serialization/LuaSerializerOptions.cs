using System;
using System.Collections.Generic;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Serialization;

/// <summary>
/// Configuration options for serializing Gmail configurations, filters, labels, and auto-reply settings to Lua.
/// </summary>
public class LuaSerializerOptions
{
    /// <summary>
    /// Gets or sets the number of spaces used per indentation level. Default is 4.
    /// </summary>
    public int IndentSpaces { get; set; } = 4;

    /// <summary>
    /// Gets or sets whether to emit a comment header at the top of the generated Lua configuration. Default is true.
    /// </summary>
    public bool IncludeHeaderComment { get; set; } = true;

    /// <summary>
    /// Gets or sets a custom header comment to prepend to the generated Lua script.
    /// </summary>
    public string? HeaderComment { get; set; }

    /// <summary>
    /// Gets or sets whether sections (labels, filters, auto_reply) that are null or empty should be omitted.
    /// Default is true.
    /// </summary>
    public bool OmitEmptySections { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to emit color aliases (e.g. "white", "black") rather than hex values when supported.
    /// Default is true.
    /// </summary>
    public bool PreferColorAliases { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to omit default false or null values for cleaner Lua representation.
    /// Default is true.
    /// </summary>
    public bool OmitDefaultValues { get; set; } = true;

    /// <summary>
    /// Known Gmail labels used to resolve internal label IDs to human-readable label names
    /// in filter actions (add_label, remove_label).
    /// </summary>
    public IEnumerable<GmailLabel>? KnownLabels { get; set; }
}
