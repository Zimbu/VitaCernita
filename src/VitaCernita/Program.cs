using System;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console;
using VitaCernita.Core.Filters;

namespace VitaCernita.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string configPath = "config/gmail_filter.lua";
        bool explicitAnd = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-c":
                case "--config":
                    if (i + 1 < args.Length) configPath = args[++i];
                    break;
                case "--explicit-and":
                    explicitAnd = true;
                    break;
                case "-v":
                case "--version":
                    AnsiConsole.MarkupLine("[bold green]VitaCernita[/] version [cyan]0.1.0[/]");
                    return 0;
                case "-h":
                case "--help":
                    PrintHelp();
                    return 0;
            }
        }

        // Header banner
        AnsiConsole.Write(
            new FigletText("VitaCernita")
                .LeftJustified()
                .Color(Color.Cyan1));

        AnsiConsole.MarkupLine("[bold]Gmail Filter Configuration Engine (Lua DSL & C# Core)[/]\n");

        // Fallback search for default config
        if (!File.Exists(configPath))
        {
            if (File.Exists("config/gmail_filter.lua")) configPath = "config/gmail_filter.lua";
            else if (File.Exists("gmail_filter.lua")) configPath = "gmail_filter.lua";
            else if (File.Exists("config/config.lua")) configPath = "config/config.lua";
            else
            {
                AnsiConsole.MarkupLine($"[bold red]Error:[/] Configuration file '[yellow]{configPath}[/]' not found.");
                return 1;
            }
        }

        AnsiConsole.MarkupLine($"Loading configuration from: [cyan]{Markup.Escape(configPath)}[/]\n");

        var loader = new GmailFilterLoader();
        try
        {
            var rules = await loader.LoadRulesFromFileAsync(configPath);
            if (rules.Count == 0)
            {
                AnsiConsole.MarkupLine("[bold yellow]No filter rules found in configuration.[/]");
                return 0;
            }

            for (int index = 0; index < rules.Count; index++)
            {
                var rule = rules[index];
                string title = !string.IsNullOrWhiteSpace(rule.Name) 
                    ? rule.Name 
                    : $"Rule #{index + 1}";

                string canonicalQuery = rule.ToGmailQuery(explicitAnd: false);
                string explicitAndQuery = rule.ToGmailQuery(explicitAnd: true);

                var panel = new Panel(
                    new Markup(
                        $"[bold white]Gmail Search Query:[/] [bold green]{Markup.Escape(explicitAnd ? explicitAndQuery : canonicalQuery)}[/]\n\n" +
                        $"[dim]Canonical (Space-AND):[/] [yellow]{Markup.Escape(canonicalQuery)}[/]\n" +
                        $"[dim]Explicit AND Keyword :[/] [yellow]{Markup.Escape(explicitAndQuery)}[/]"
                    ))
                {
                    Header = new PanelHeader($"[bold cyan]{Markup.Escape(title)}[/]"),
                    Border = BoxBorder.Rounded,
                    Padding = new Padding(2, 1)
                };

                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();

                // Directly output the exact search text for easy copying/piping
                AnsiConsole.MarkupLine("[bold]Precise Gmail Filter Text:[/] [green]" + Markup.Escape(canonicalQuery) + "[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[bold red]Error loading filter configuration:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static void PrintHelp()
    {
        AnsiConsole.MarkupLine("[bold]VitaCernita CLI - Gmail Filter Manager[/]");
        AnsiConsole.MarkupLine("Usage: dotnet run --project src/VitaCernita -- [OPTIONS]\n");
        AnsiConsole.MarkupLine("[bold]Options:[/]");
        AnsiConsole.MarkupLine("  -c, --config <path>     Path to the Lua filter configuration file (default: config/gmail_filter.lua)");
        AnsiConsole.MarkupLine("      --explicit-and      Render queries using the explicit 'AND' keyword");
        AnsiConsole.MarkupLine("  -v, --version           Display application version");
        AnsiConsole.MarkupLine("  -h, --help              Show this help message");
    }
}
