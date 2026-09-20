using System;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using VitaCernita.Cli.Engine;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// Default CLI command executed when no specific subcommand is provided.
/// Encapsulates global help output, version display, and top-level options.
/// </summary>
[Command(Description = "VitaCernita CLI - Gmail Filter, Label & Auto-Reply Manager")]
public class DefaultCommand : ICliCommand, IDispatcherAware
{
    private readonly IAnsiConsole _console;
    private CommandDispatcher? _dispatcher;

    public DefaultCommand(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public void SetDispatcher(CommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return Task.FromResult(0);
        }

        string firstArg = args[0];
        switch (firstArg)
        {
            case "-v":
            case "--version":
                PrintVersion();
                return Task.FromResult(0);

            case "-h":
            case "--help":
                PrintHelp();
                return Task.FromResult(0);

            default:
                _console.MarkupLine($"[bold red]Error:[/] Unknown option '[yellow]{Markup.Escape(firstArg)}[/]'. Run 'vitacernita --help' for available options.");
                return Task.FromResult(1);
        }
    }

    public void PrintHelp()
    {
        _console.MarkupLine("[bold]VitaCernita CLI - Gmail Filter, Label & Auto-Reply Manager[/]");
        _console.MarkupLine("Usage: vitacernita <command> [[options]]\n");
        _console.MarkupLine("[bold]Available Commands:[/]");

        var commands = (_dispatcher?.Commands ?? Array.Empty<ICliCommand>())
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .ToList();

        int maxNameLen = commands.Count > 0 ? commands.Max(c => c.Name.Length) : 10;
        if (maxNameLen < 4) maxNameLen = 4;

        foreach (var cmd in commands)
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

    public void PrintVersion()
    {
        _console.MarkupLine("[bold green]VitaCernita[/] version [cyan]0.1.0[/]");
    }
}
