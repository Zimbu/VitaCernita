using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VitaCernita.Cli.Engine.Binding;

/// <summary>
/// Binds CLI arguments to properties decorated with <see cref="OptionAttribute"/> on an <see cref="ICliCommand"/>.
/// </summary>
public class CommandParameterBinder
{
    private static readonly ConcurrentDictionary<Type, CommandMetadata> _metadataCache = new();

    public static CommandParameterBinder Default { get; } = new();

    public BindResult Bind(ICliCommand command, string[] args)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(args);

        var metadata = GetMetadata(command.GetType());
        var boundDescriptors = new HashSet<OptionDescriptor>();
        var unhandledArguments = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg == "--")
            {
                // Everything after '--' is treated as positional/unhandled
                for (int j = i + 1; j < args.Length; j++)
                {
                    unhandledArguments.Add(args[j]);
                }
                break;
            }

            if (arg is "-h" or "--help")
            {
                return BindResult.Help();
            }

            if (arg.StartsWith("--") && arg.Length > 2)
            {
                string raw = arg[2..];
                string name;
                string? explicitValue = null;
                int eqIdx = raw.IndexOf('=');

                if (eqIdx >= 0)
                {
                    name = raw[..eqIdx];
                    explicitValue = raw[(eqIdx + 1)..];
                }
                else
                {
                    name = raw;
                }

                // Check boolean negation syntax: --no-<switch>
                bool isNegated = false;
                if (name.StartsWith("no-", StringComparison.OrdinalIgnoreCase))
                {
                    string candidate = name[3..];
                    if (metadata.LongOptions.TryGetValue(candidate, out var negDesc) && negDesc.IsSwitch)
                    {
                        name = candidate;
                        isNegated = true;
                        explicitValue = "false";
                    }
                }

                if (!metadata.LongOptions.TryGetValue(name, out var descriptor))
                {
                    string cmdName = command.Name;
                    string appName = CommandDispatcher.ResolveApplicationName(command.GetType().Assembly);
                    string helpHint = !string.IsNullOrWhiteSpace(cmdName)
                        ? $"Run '{appName} {cmdName} --help' for available options."
                        : $"Run '{appName} --help' for available options.";
                    return BindResult.Failure($"Unknown option '{arg}'. {helpHint}");
                }

                if (descriptor.IsSwitch)
                {
                    bool boolVal = true;
                    if (explicitValue != null)
                    {
                        if (!bool.TryParse(explicitValue, out boolVal))
                        {
                            return BindResult.Failure($"Invalid value '{explicitValue}' for boolean switch '--{name}'.");
                        }
                    }
                    else if (isNegated)
                    {
                        boolVal = false;
                    }

                    descriptor.Property.SetValue(command, boolVal);
                    boundDescriptors.Add(descriptor);
                }
                else
                {
                    string valueToParse;
                    if (explicitValue != null)
                    {
                        valueToParse = explicitValue;
                    }
                    else
                    {
                        if (i + 1 >= args.Length)
                        {
                            return BindResult.Failure($"Missing value for option '--{name}'.");
                        }
                        valueToParse = args[++i];
                    }

                    var parseResult = ConvertValue(valueToParse, descriptor.TargetType, descriptor.UnderlyingType, descriptor.EffectiveName);
                    if (!parseResult.IsSuccess)
                    {
                        return BindResult.Failure(parseResult.ErrorMessage!);
                    }

                    AssignValue(command, descriptor, parseResult.Value);
                    boundDescriptors.Add(descriptor);
                }
            }
            else if (arg.StartsWith("-") && arg.Length > 1)
            {
                char shortChar = arg[1];

                if (arg.Length > 2 && arg[2] == '=')
                {
                    string explicitValue = arg[3..];
                    if (!metadata.ShortOptions.TryGetValue(shortChar, out var descriptor))
                    {
                        return BindResult.Failure($"Unknown short option '-{shortChar}'.");
                    }

                    if (descriptor.IsSwitch)
                    {
                        if (!bool.TryParse(explicitValue, out bool boolVal))
                        {
                            return BindResult.Failure($"Invalid value '{explicitValue}' for boolean switch '-{shortChar}'.");
                        }
                        descriptor.Property.SetValue(command, boolVal);
                        boundDescriptors.Add(descriptor);
                    }
                    else
                    {
                        var parseResult = ConvertValue(explicitValue, descriptor.TargetType, descriptor.UnderlyingType, descriptor.EffectiveName);
                        if (!parseResult.IsSuccess)
                        {
                            return BindResult.Failure(parseResult.ErrorMessage!);
                        }
                        AssignValue(command, descriptor, parseResult.Value);
                        boundDescriptors.Add(descriptor);
                    }
                }
                else if (arg.Length == 2)
                {
                    if (!metadata.ShortOptions.TryGetValue(shortChar, out var descriptor))
                    {
                        return BindResult.Failure($"Unknown short option '-{shortChar}'.");
                    }

                    if (descriptor.IsSwitch)
                    {
                        descriptor.Property.SetValue(command, true);
                        boundDescriptors.Add(descriptor);
                    }
                    else
                    {
                        if (i + 1 >= args.Length)
                        {
                            return BindResult.Failure($"Missing value for option '-{shortChar}'.");
                        }
                        string valueToParse = args[++i];
                        var parseResult = ConvertValue(valueToParse, descriptor.TargetType, descriptor.UnderlyingType, descriptor.EffectiveName);
                        if (!parseResult.IsSuccess)
                        {
                            return BindResult.Failure(parseResult.ErrorMessage!);
                        }
                        AssignValue(command, descriptor, parseResult.Value);
                        boundDescriptors.Add(descriptor);
                    }
                }
                else
                {
                    // Check for bundled short switches (e.g. -fa for -f and -a)
                    bool allBundledSwitches = true;
                    for (int k = 1; k < arg.Length; k++)
                    {
                        if (!metadata.ShortOptions.TryGetValue(arg[k], out var d) || !d.IsSwitch)
                        {
                            allBundledSwitches = false;
                            break;
                        }
                    }

                    if (allBundledSwitches)
                    {
                        for (int k = 1; k < arg.Length; k++)
                        {
                            var d = metadata.ShortOptions[arg[k]];
                            d.Property.SetValue(command, true);
                            boundDescriptors.Add(d);
                        }
                    }
                    else if (metadata.ShortOptions.TryGetValue(shortChar, out var valueDesc) && !valueDesc.IsSwitch)
                    {
                        // Syntax: -cconfig.lua (attached value)
                        string attachedValue = arg[2..];
                        var parseResult = ConvertValue(attachedValue, valueDesc.TargetType, valueDesc.UnderlyingType, valueDesc.EffectiveName);
                        if (!parseResult.IsSuccess)
                        {
                            return BindResult.Failure(parseResult.ErrorMessage!);
                        }
                        AssignValue(command, valueDesc, parseResult.Value);
                        boundDescriptors.Add(valueDesc);
                    }
                    else
                    {
                        return BindResult.Failure($"Unknown option '{arg}'.");
                    }
                }
            }
            else
            {
                unhandledArguments.Add(arg);
            }
        }

        // Validate required options
        foreach (var descriptor in metadata.Descriptors)
        {
            if (descriptor.Required && !boundDescriptors.Contains(descriptor))
            {
                var currentVal = descriptor.Property.GetValue(command);
                if (currentVal == null || (currentVal is string s && string.IsNullOrWhiteSpace(s)))
                {
                    return BindResult.Failure($"Missing required option '--{descriptor.EffectiveName}'.");
                }
            }
        }

        return BindResult.Success(unhandledArguments);
    }

    public static IReadOnlyList<OptionDescriptor> GetDescriptors(Type commandType)
    {
        return GetMetadata(commandType).Descriptors;
    }

    private static CommandMetadata GetMetadata(Type commandType)
    {
        return _metadataCache.GetOrAdd(commandType, type =>
        {
            var descriptors = new List<OptionDescriptor>();
            var longOptions = new Dictionary<string, OptionDescriptor>(StringComparer.OrdinalIgnoreCase);
            var shortOptions = new Dictionary<char, OptionDescriptor>();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetCustomAttribute<OptionAttribute>() != null);

            // Create a dummy instance if possible to capture default property values for help
            object? dummyInstance = null;
            try
            {
                dummyInstance = Activator.CreateInstance(type);
            }
            catch
            {
                // If no parameterless ctor, dummyInstance remains null
            }

            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<OptionAttribute>()!;
                string effectiveName = !string.IsNullOrWhiteSpace(attr.Name)
                    ? attr.Name
                    : ToKebabCase(prop.Name);

                Type targetType = prop.PropertyType;
                Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                bool isSwitch = underlyingType == typeof(bool);

                object? defaultValue = null;
                if (dummyInstance != null)
                {
                    try { defaultValue = prop.GetValue(dummyInstance); } catch { }
                }

                var descriptor = new OptionDescriptor(
                    prop,
                    attr,
                    effectiveName,
                    attr.ShortName,
                    attr.Aliases,
                    targetType,
                    underlyingType,
                    isSwitch,
                    attr.Required,
                    attr.Description,
                    attr.ValueHelp,
                    defaultValue);

                // Intra-command uniqueness validation: Long name
                if (longOptions.TryGetValue(effectiveName, out var existingLong))
                {
                    throw new InvalidOperationException(
                        $"Command '{type.Name}' defines duplicate option name '--{effectiveName}' on properties '{existingLong.Property.Name}' and '{prop.Name}'.");
                }
                longOptions[effectiveName] = descriptor;

                // Intra-command uniqueness validation: Aliases
                var seenAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var alias in attr.Aliases)
                {
                    if (string.IsNullOrWhiteSpace(alias))
                    {
                        throw new InvalidOperationException(
                            $"Command '{type.Name}' defines an empty alias on property '{prop.Name}'.");
                    }

                    if (string.Equals(alias, effectiveName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Command '{type.Name}' defines an alias '--{alias}' on property '{prop.Name}' that matches its primary name.");
                    }

                    if (!seenAliases.Add(alias))
                    {
                        throw new InvalidOperationException(
                            $"Command '{type.Name}' defines duplicate alias '--{alias}' on property '{prop.Name}'.");
                    }

                    if (longOptions.TryGetValue(alias, out var existingAlias))
                    {
                        throw new InvalidOperationException(
                            $"Command '{type.Name}' defines alias '--{alias}' on property '{prop.Name}' which conflicts with an option on '{existingAlias.Property.Name}'.");
                    }
                    longOptions[alias] = descriptor;
                }

                // Intra-command uniqueness validation: Short name
                if (attr.ShortName != '\0')
                {
                    if (shortOptions.TryGetValue(attr.ShortName, out var existingShort))
                    {
                        throw new InvalidOperationException(
                            $"Command '{type.Name}' defines duplicate short alias '-{attr.ShortName}' on properties '{existingShort.Property.Name}' and '{prop.Name}'.");
                    }
                    shortOptions[attr.ShortName] = descriptor;
                }

                descriptors.Add(descriptor);
            }

            return new CommandMetadata(descriptors, longOptions, shortOptions);
        });
    }

    private static (bool IsSuccess, object? Value, string? ErrorMessage) ConvertValue(
        string rawValue,
        Type targetType,
        Type underlyingType,
        string optionName)
    {
        if (underlyingType == typeof(string))
        {
            return (true, rawValue, null);
        }

        if (underlyingType == typeof(bool))
        {
            if (bool.TryParse(rawValue, out bool bVal)) return (true, bVal, null);
            if (rawValue == "1") return (true, true, null);
            if (rawValue == "0") return (true, false, null);
            return (false, null, $"Invalid boolean value '{rawValue}' for option '--{optionName}'.");
        }

        if (underlyingType.IsEnum)
        {
            if (Enum.TryParse(underlyingType, rawValue, ignoreCase: true, out object? enumVal))
            {
                return (true, enumVal, null);
            }
            string validValues = string.Join(", ", Enum.GetNames(underlyingType));
            return (false, null, $"Invalid value '{rawValue}' for option '--{optionName}'. Valid values are: {validValues}.");
        }

        // Generic collection / list handling: List<string> or string[]
        if (targetType == typeof(List<string>))
        {
            var list = rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            return (true, list, null);
        }

        if (targetType == typeof(string[]))
        {
            var array = rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return (true, array, null);
        }

        if (underlyingType == typeof(Guid))
        {
            if (Guid.TryParse(rawValue, out var guidVal)) return (true, guidVal, null);
            return (false, null, $"Invalid GUID '{rawValue}' for option '--{optionName}'.");
        }

        if (underlyingType == typeof(DateTime))
        {
            if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dtVal))
                return (true, dtVal, null);
            return (false, null, $"Invalid date/time '{rawValue}' for option '--{optionName}'.");
        }

        try
        {
            object converted = Convert.ChangeType(rawValue, underlyingType, CultureInfo.InvariantCulture);
            return (true, converted, null);
        }
        catch
        {
            string typeName = underlyingType.Name.ToLowerInvariant();
            return (false, null, $"Invalid value '{rawValue}' for option '--{optionName}': expected a valid {typeName}.");
        }
    }

    private static void AssignValue(ICliCommand command, OptionDescriptor descriptor, object? value)
    {
        if (descriptor.TargetType == typeof(List<string>) && value is List<string> incomingList)
        {
            var existing = descriptor.Property.GetValue(command) as List<string>;
            if (existing != null)
            {
                existing.AddRange(incomingList);
                return;
            }
        }

        descriptor.Property.SetValue(command, value);
    }

    public static string ToKebabCase(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        var sb = new StringBuilder();
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && str[i - 1] != '-')
                {
                    sb.Append('-');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private sealed class CommandMetadata
    {
        public IReadOnlyList<OptionDescriptor> Descriptors { get; }
        public IReadOnlyDictionary<string, OptionDescriptor> LongOptions { get; }
        public IReadOnlyDictionary<char, OptionDescriptor> ShortOptions { get; }

        public CommandMetadata(
            IReadOnlyList<OptionDescriptor> descriptors,
            IReadOnlyDictionary<string, OptionDescriptor> longOptions,
            IReadOnlyDictionary<char, OptionDescriptor> shortOptions)
        {
            Descriptors = descriptors;
            LongOptions = longOptions;
            ShortOptions = shortOptions;
        }
    }
}
