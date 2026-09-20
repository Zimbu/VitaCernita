using System;
using System.Collections.Generic;
using System.Reflection;

namespace VitaCernita.Cli.Commands.Binding;

/// <summary>
/// Cached reflection metadata for a command's option property.
/// </summary>
public sealed class OptionDescriptor
{
    public PropertyInfo Property { get; }
    public OptionAttribute Attribute { get; }
    public string EffectiveName { get; }
    public char ShortName { get; }
    public IReadOnlyList<string> Aliases { get; }
    public Type TargetType { get; }
    public Type UnderlyingType { get; }
    public bool IsSwitch { get; }
    public bool Required { get; }
    public string Description { get; }
    public string? ValueHelp { get; }
    public object? DefaultValue { get; }

    public OptionDescriptor(
        PropertyInfo property,
        OptionAttribute attribute,
        string effectiveName,
        char shortName,
        IReadOnlyList<string> aliases,
        Type targetType,
        Type underlyingType,
        bool isSwitch,
        bool required,
        string description,
        string? valueHelp,
        object? defaultValue)
    {
        Property = property;
        Attribute = attribute;
        EffectiveName = effectiveName;
        ShortName = shortName;
        Aliases = aliases;
        TargetType = targetType;
        UnderlyingType = underlyingType;
        IsSwitch = isSwitch;
        Required = required;
        Description = description;
        ValueHelp = valueHelp;
        DefaultValue = defaultValue;
    }
}
