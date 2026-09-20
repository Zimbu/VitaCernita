using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Spectre.Console;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// Routes CLI invocations to registered subcommands with global uniqueness validation
/// and single default command support. Fully generic and decoupled from any specific application.
/// </summary>
public class CommandDispatcher
{
    private readonly Dictionary<string, ICliCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (ICliCommand Command, bool IsAlias)> _registeredTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ICliCommand> _orderedCommands = new();
    private readonly IAnsiConsole _console;
    private readonly Binding.CommandParameterBinder _binder;
    private ICliCommand? _defaultCommand;

    public string ApplicationName { get; private set; }

    public CommandDispatcher(
        IAnsiConsole? console = null,
        Binding.CommandParameterBinder? binder = null,
        string? applicationName = null)
    {
        _console = console ?? AnsiConsole.Console;
        _binder = binder ?? Binding.CommandParameterBinder.Default;
        ApplicationName = applicationName ?? ResolveApplicationName();
    }

    public IReadOnlyList<ICliCommand> Commands => _orderedCommands;

    public ICliCommand? DefaultCommand => _defaultCommand;

    /// <summary>
    /// Registers a command by interrogating its <see cref="CommandAttribute"/> or properties.
    /// </summary>
    public CommandDispatcher Register(ICliCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var attr = command.GetType().GetCustomAttribute<CommandAttribute>();
        return RegisterInternal(command, attr);
    }

    /// <summary>
    /// Registers a command with an explicit <see cref="CommandAttribute"/>.
    /// </summary>
    public CommandDispatcher Register(CommandAttribute attribute, ICliCommand command)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(command);
        return RegisterInternal(command, attribute);
    }

    /// <summary>
    /// Scans an assembly for non-abstract classes implementing <see cref="ICliCommand"/>
    /// decorated with <see cref="CommandAttribute"/>, instantiating and registering each.
    /// </summary>
    public CommandDispatcher RegisterFromAssembly(Assembly assembly, Func<Type, ICliCommand>? factory = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (string.Equals(ApplicationName, "app", StringComparison.OrdinalIgnoreCase))
        {
            ApplicationName = ResolveApplicationName(assembly);
        }

        var commandTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ICliCommand).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttribute<CommandAttribute>() != null)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var type in commandTypes)
        {
            var command = CreateCommandInstance(type, factory);
            Register(command);
        }

        return this;
    }

    public ICliCommand? GetCommand(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return _defaultCommand;
        }

        _commands.TryGetValue(name, out var command);
        return command;
    }

    public async Task<int> DispatchAsync(string[] args)
    {
        if (args.Length == 0)
        {
            if (_defaultCommand != null)
            {
                return await _defaultCommand.ExecuteAsync(args);
            }

            PrintGlobalHelp();
            return 0;
        }

        string firstArg = args[0];

        // Global version check
        if (firstArg is "-v" or "--version")
        {
            if (_defaultCommand != null)
            {
                return await _defaultCommand.ExecuteAsync(args);
            }

            string version = GetType().Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
            _console.MarkupLine($"[bold green]{Markup.Escape(ApplicationName)}[/] version [cyan]{Markup.Escape(version)}[/]");
            return 0;
        }

        // Global help check
        if (firstArg is "-h" or "--help")
        {
            if (_defaultCommand != null)
            {
                return await _defaultCommand.ExecuteAsync(args);
            }

            PrintGlobalHelp();
            return 0;
        }

        // 'help <command>' check
        if (firstArg.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length > 1)
            {
                var targetCommand = GetCommand(args[1]);
                if (targetCommand != null)
                {
                    targetCommand.PrintHelp();
                    return 0;
                }

                _console.MarkupLine($"[bold red]Error:[/] Unknown command '[yellow]{Markup.Escape(args[1])}[/]'. Run '{Markup.Escape(ApplicationName)} --help' for available commands.");
                return 1;
            }

            if (_defaultCommand != null)
            {
                return await _defaultCommand.ExecuteAsync(Array.Empty<string>());
            }

            PrintGlobalHelp();
            return 0;
        }

        // Explicit subcommand invocation
        var cmd = GetCommand(firstArg);
        if (cmd != null)
        {
            string[] subArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();

            var bindResult = _binder.Bind(cmd, subArgs);
            if (bindResult.HelpRequested)
            {
                cmd.PrintHelp();
                return 0;
            }

            if (!bindResult.IsSuccess)
            {
                _console.MarkupLine($"[bold red]Error:[/] {bindResult.ErrorMessage}");
                return 1;
            }

            return await cmd.ExecuteAsync(subArgs);
        }

        // Flags provided without subcommand: delegate to default command
        if (firstArg.StartsWith("-"))
        {
            if (_defaultCommand != null)
            {
                return await _defaultCommand.ExecuteAsync(args);
            }

            _console.MarkupLine($"[bold red]Error:[/] Unknown option '[yellow]{Markup.Escape(firstArg)}[/]'. Run '{Markup.Escape(ApplicationName)} --help' for available options.");
            return 1;
        }

        _console.MarkupLine($"[bold red]Error:[/] Unknown command '[yellow]{Markup.Escape(firstArg)}[/]'. Run '{Markup.Escape(ApplicationName)} --help' for available commands.");
        return 1;
    }

    public void PrintGlobalHelp()
    {
        if (_defaultCommand != null)
        {
            _defaultCommand.PrintHelp();
            return;
        }

        _console.MarkupLine($"[bold]{Markup.Escape(ApplicationName)} CLI[/]");
        _console.MarkupLine($"Usage: {Markup.Escape(ApplicationName)} <command> [[options]]\n");
        _console.MarkupLine("[bold]Available Commands:[/]");

        int maxNameLen = _orderedCommands.Count > 0 ? _orderedCommands.Max(c => c.Name.Length) : 10;
        if (maxNameLen < 4) maxNameLen = 4;

        foreach (var cmd in _orderedCommands)
        {
            string aliasInfo = cmd.Aliases.Count > 0 ? $" (aliases: {string.Join(", ", cmd.Aliases)})" : "";
            _console.MarkupLine($"  [cyan]{cmd.Name.PadRight(maxNameLen + 2)}[/] {cmd.Description}{aliasInfo}");
        }
        _console.MarkupLine($"  [cyan]{"help".PadRight(maxNameLen + 2)}[/] Show help details for a command (e.g. '{Markup.Escape(ApplicationName)} help <command>')\n");

        _console.MarkupLine("[bold]Global Options:[/]");
        _console.MarkupLine("  -v, --version           Display application version");
        _console.MarkupLine("  -h, --help              Show this help message\n");
        _console.MarkupLine($"Run '[cyan]{Markup.Escape(ApplicationName)} <command> --help[/]' for detailed options on a specific command.");
    }

    private CommandDispatcher RegisterInternal(ICliCommand command, CommandAttribute? attr)
    {
        // Validate option declarations on the command type
        Binding.CommandParameterBinder.GetDescriptors(command.GetType());

        string name = attr?.Name ?? command.Name;
        IReadOnlyList<string> aliases = (attr != null && attr.Aliases.Length > 0) ? attr.Aliases : command.Aliases;
        bool isDefault = (attr != null && attr.IsDefault) || string.IsNullOrWhiteSpace(name);

        if (isDefault)
        {
            if (_defaultCommand != null)
            {
                throw new InvalidOperationException(
                    $"A default command '{_defaultCommand.GetType().Name}' is already registered. Cannot register '{command.GetType().Name}' as default command.");
            }

            if (aliases.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Default command '{command.GetType().Name}' cannot have aliases.");
            }

            _defaultCommand = command;
            if (command is IDispatcherAware da)
            {
                da.SetDispatcher(this);
            }
            return this;
        }

        // Validate command name uniqueness
        if (_registeredTokens.TryGetValue(name, out var existingNameToken))
        {
            if (existingNameToken.IsAlias)
            {
                throw new InvalidOperationException(
                    $"Cannot register command '{command.GetType().Name}' with name '{name}' because it conflicts with an alias registered by '{existingNameToken.Command.GetType().Name}'.");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot register command '{command.GetType().Name}' with name '{name}' because a command with that name is already registered by '{existingNameToken.Command.GetType().Name}'.");
            }
        }

        // Validate aliases uniqueness and validity
        var seenAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                throw new InvalidOperationException(
                    $"Command '{command.GetType().Name}' defines an empty or whitespace alias.");
            }

            if (string.Equals(alias, name, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Command '{command.GetType().Name}' defines an alias '{alias}' that matches its primary name.");
            }

            if (!seenAliases.Add(alias))
            {
                throw new InvalidOperationException(
                    $"Command '{command.GetType().Name}' defines duplicate alias '{alias}'.");
            }

            if (_registeredTokens.TryGetValue(alias, out var existingAliasToken))
            {
                if (existingAliasToken.IsAlias)
                {
                    throw new InvalidOperationException(
                        $"Cannot register alias '{alias}' on command '{command.GetType().Name}' because it conflicts with an alias registered by '{existingAliasToken.Command.GetType().Name}'.");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Cannot register alias '{alias}' on command '{command.GetType().Name}' because it conflicts with the command name of '{existingAliasToken.Command.GetType().Name}'.");
                }
            }
        }

        // All validations passed; record mappings
        _registeredTokens[name] = (command, false);
        _commands[name] = command;

        foreach (var alias in aliases)
        {
            _registeredTokens[alias] = (command, true);
            _commands[alias] = command;
        }

        _orderedCommands.Add(command);

        if (command is IDispatcherAware dispatcherAware)
        {
            dispatcherAware.SetDispatcher(this);
        }

        return this;
    }

    private ICliCommand CreateCommandInstance(Type type, Func<Type, ICliCommand>? factory)
    {
        if (factory != null)
        {
            return factory(type);
        }

        var ctors = type.GetConstructors();

        // 1. Single constructor parameter of type IAnsiConsole
        foreach (var ctor in ctors)
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(IAnsiConsole))
            {
                return (ICliCommand)ctor.Invoke(new object?[] { _console });
            }
        }

        // 2. Constructor where all parameters are either IAnsiConsole or optional
        foreach (var ctor in ctors)
        {
            var parameters = ctor.GetParameters();
            if (parameters.All(p => p.IsOptional || p.ParameterType == typeof(IAnsiConsole)))
            {
                var args = parameters
                    .Select(p => p.ParameterType == typeof(IAnsiConsole) ? (object?)_console : Type.Missing)
                    .ToArray();
                return (ICliCommand)ctor.Invoke(args);
            }
        }

        // 3. Fallback to parameterless Activator
        return (ICliCommand)Activator.CreateInstance(type)!;
    }

    public static string ResolveApplicationName(Assembly? targetAssembly = null)
    {
        try
        {
            string[] cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length > 0 && !string.IsNullOrWhiteSpace(cmdArgs[0]))
            {
                string name = Path.GetFileNameWithoutExtension(cmdArgs[0]);
                if (!name.Equals("dotnet", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("testhost", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("vstest", StringComparison.OrdinalIgnoreCase))
                {
                    return name.ToLowerInvariant();
                }
            }
        }
        catch
        {
            // Ignore security or argument exceptions in restricted environments
        }

        var entry = targetAssembly ?? Assembly.GetEntryAssembly();
        if (entry != null)
        {
            string? name = entry.GetName().Name;
            if (!string.IsNullOrWhiteSpace(name) &&
                !name.Contains("testhost", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("vstest", StringComparison.OrdinalIgnoreCase) &&
                !name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
            {
                return name.ToLowerInvariant();
            }
        }

        if (targetAssembly != null)
        {
            string? name = targetAssembly.GetName().Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                int dotIndex = name.IndexOf('.');
                string baseName = dotIndex > 0 ? name[..dotIndex] : name;
                return baseName.ToLowerInvariant();
            }
        }

        return "app";
    }
}
