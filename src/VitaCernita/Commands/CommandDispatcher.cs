using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// Routes CLI invocations to registered subcommands with backward-compatible fallback.
/// </summary>
public class CommandDispatcher
{
    private readonly Dictionary<string, ICliCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ICliCommand> _orderedCommands = new();
    private readonly IAnsiConsole _console;

    public CommandDispatcher(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public IReadOnlyList<ICliCommand> Commands => _orderedCommands;

    public CommandDispatcher Register(ICliCommand command)
    {
        _orderedCommands.Add(command);
        _commands[command.Name] = command;
        foreach (var alias in command.Aliases)
        {
            _commands[alias] = command;
        }
        return this;
    }

    public ICliCommand? GetCommand(string name)
    {
        _commands.TryGetValue(name, out var command);
        return command;
    }

    public async Task<int> DispatchAsync(string[] args)
    {
        if (args.Length == 0)
        {
            _console.MarkupLine("[dim]Note: Defaulting to 'test' command. In the future, use 'vitacernita test'.[/]");
            return await ExecuteCommandAsync("test", Array.Empty<string>());
        }

        string firstArg = args[0];

        // Global version check
        if (firstArg is "-v" or "--version")
        {
            _console.MarkupLine("[bold green]VitaCernita[/] version [cyan]0.1.0[/]");
            return 0;
        }

        // Global help check
        if (firstArg is "-h" or "--help")
        {
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

                _console.MarkupLine($"[bold red]Error:[/] Unknown command '[yellow]{Markup.Escape(args[1])}[/]'. Run 'vitacernita --help' for available commands.");
                return 1;
            }

            PrintGlobalHelp();
            return 0;
        }

        // Explicit subcommand invocation
        var cmd = GetCommand(firstArg);
        if (cmd != null)
        {
            string[] subArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();
            return await cmd.ExecuteAsync(subArgs);
        }

        // Legacy invocation fallback: flags provided without subcommand
        if (firstArg.StartsWith("-"))
        {
            _console.MarkupLine("[dim]Note: Defaulting to 'test' command. In the future, use 'vitacernita test'.[/]");
            return await ExecuteCommandAsync("test", args);
        }

        _console.MarkupLine($"[bold red]Error:[/] Unknown command '[yellow]{Markup.Escape(firstArg)}[/]'. Run 'vitacernita --help' for available commands.");
        return 1;
    }

    private async Task<int> ExecuteCommandAsync(string commandName, string[] args)
    {
        var cmd = GetCommand(commandName);
        if (cmd == null)
        {
            _console.MarkupLine($"[bold red]Error:[/] Internal command '{commandName}' not found.");
            return 1;
        }
        return await cmd.ExecuteAsync(args);
    }

    public void PrintGlobalHelp()
    {
        _console.MarkupLine("[bold]VitaCernita CLI - Gmail Filter, Label & Auto-Reply Manager[/]");
        _console.MarkupLine("Usage: vitacernita <command> [[options]]\n");
        _console.MarkupLine("[bold]Available Commands:[/]");

        int maxNameLen = _orderedCommands.Count > 0 ? _orderedCommands.Max(c => c.Name.Length) : 10;
        foreach (var cmd in _orderedCommands)
        {
            string aliasInfo = cmd.Aliases.Count > 0 ? $" (aliases: {string.Join(", ", cmd.Aliases)})" : "";
            _console.MarkupLine($"  [cyan]{cmd.Name.PadRight(maxNameLen + 2)}[/] {cmd.Description}{aliasInfo}");
        }
        _console.MarkupLine($"  [cyan]{"help".PadRight(maxNameLen + 2)}[/] Show help details for a command (e.g. 'vitacernita help <command>')\n");

        _console.MarkupLine("[bold]Global Options:[/]");
        _console.MarkupLine("  -v, --version           Display application version");
        _console.MarkupLine("  -h, --help              Show this help message\n");
        _console.MarkupLine("Run '[cyan]vitacernita <command> --help[/]' for detailed options on a specific command.");
    }
}
