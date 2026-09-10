using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lua;
using Spectre.Console;
using VitaCernita.Core.Configuration;
using VitaCernita.Core.Models;
using VitaCernita.Core.Services;

namespace VitaCernita.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Parse basic CLI arguments
        string configPath = "config/config.lua";
        string command = "run";
        string? evalCode = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-c":
                case "--config":
                    if (i + 1 < args.Length) configPath = args[++i];
                    break;
                case "-v":
                case "--version":
                    AnsiConsole.MarkupLine("[bold green]VitaCernita[/] version [cyan]0.1.0[/]");
                    return 0;
                case "-h":
                case "--help":
                    PrintHelp();
                    return 0;
                case "validate":
                    command = "validate";
                    break;
                case "run":
                    command = "run";
                    break;
                case "eval":
                    command = "eval";
                    if (i + 1 < args.Length) evalCode = args[++i];
                    break;
            }
        }

        // Handle "eval" command
        if (command == "eval")
        {
            return await ExecuteEvalAsync(evalCode);
        }

        // Render header banner
        AnsiConsole.Write(
            new FigletText("VitaCernita")
                .LeftJustified()
                .Color(Color.Cyan1));

        AnsiConsole.MarkupLine("[bold]Cross-Platform Core C# (.NET 9) with Lua Configuration[/]\n");

        // Locate config file
        if (!File.Exists(configPath))
        {
            if (File.Exists("config.lua")) configPath = "config.lua";
            else if (File.Exists("config/config.example.lua")) configPath = "config/config.example.lua";
            else
            {
                AnsiConsole.MarkupLine($"[bold red]Error:[/] Configuration file '[yellow]{configPath}[/]' not found.");
                return 1;
            }
        }

        var loader = new LuaConfigLoader();
        var luaState = LuaState.Create();
        AppConfig config;

        try
        {
            config = await loader.LoadFromFileAsync(configPath, luaState);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[bold red]Configuration Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        // Print loaded configuration table
        RenderConfigTable(configPath, config);

        if (command == "validate")
        {
            AnsiConsole.MarkupLine("\n[bold green]✓ Configuration validated successfully![/]");
            return 0;
        }

        // Execute Triage Engine
        AnsiConsole.MarkupLine("\n[bold yellow]Executing Triage Engine...[/]");
        var engine = new TriageEngine(config, luaState);

        var sampleItems = new[]
        {
            new TriageItem { Title = "Critical production incident response", Urgency = 10, Effort = 4 },
            new TriageItem { Title = "Update dependency versions", Urgency = 4, Effort = 2 },
            new TriageItem { Title = "Refactor legacy telemetry parser", Urgency = 6, Effort = 8 },
            new TriageItem { Title = "Clean up quarterly build artifacts", Urgency = 2, Effort = 1 }
        };

        var resultsTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Triage Processing Results (Scored by Lua)[/]")
            .AddColumn("[bold]ID[/]")
            .AddColumn("[bold]Item Title[/]")
            .AddColumn("[bold]Urgency[/]", c => c.RightAligned())
            .AddColumn("[bold]Effort[/]", c => c.RightAligned())
            .AddColumn("[bold]Score (Lua Hook)[/]", c => c.RightAligned())
            .AddColumn("[bold]Assigned Category[/]");

        foreach (var item in sampleItems)
        {
            var processed = await engine.ProcessItemAsync(item);
            string scoreColor = processed.Score > 15 ? "red" : processed.Score > 5 ? "yellow" : "green";

            resultsTable.AddRow(
                $"[dim]{processed.Id}[/]",
                Markup.Escape(processed.Title),
                processed.Urgency.ToString(),
                processed.Effort.ToString(),
                $"[{scoreColor}]{processed.Score:F1}[/]",
                $"[blue]{processed.Category}[/]"
            );
        }

        AnsiConsole.Write(resultsTable);
        AnsiConsole.MarkupLine("\n[bold green]✓ VitaCernita completed successfully.[/]");
        return 0;
    }

    private static void RenderConfigTable(string configPath, AppConfig config)
    {
        var metaTable = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]");

        metaTable.AddRow("Config Path", $"[cyan]{Markup.Escape(configPath)}[/]");
        metaTable.AddRow("Project Name", config.Project.Name);
        metaTable.AddRow("Version", config.Project.Version);
        metaTable.AddRow("Author", config.Project.Author);
        metaTable.AddRow("Log Level", config.Settings.LogLevel);
        metaTable.AddRow("Max Concurrency", config.Settings.MaxConcurrency.ToString());
        metaTable.AddRow("Output Dir", config.Settings.OutputDirectory);
        metaTable.AddRow("Loaded Rules Count", config.Rules.Count.ToString());

        foreach (var custom in config.CustomProperties)
        {
            metaTable.AddRow($"Custom: {custom.Key}", custom.Value);
        }

        AnsiConsole.Write(metaTable);
    }

    private static async Task<int> ExecuteEvalAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            AnsiConsole.MarkupLine("[bold red]Error:[/] No Lua code provided to evaluate.");
            return 1;
        }

        try
        {
            var state = LuaState.Create();
            var results = await state.DoStringAsync(code);
            AnsiConsole.MarkupLine("[bold green]Lua Evaluation Result:[/]");
            for (int i = 0; i < results.Length; i++)
            {
                AnsiConsole.MarkupLine($"  [{i}]: [cyan]{Markup.Escape(results[i].ToString())}[/]");
            }
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[bold red]Lua Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static void PrintHelp()
    {
        AnsiConsole.MarkupLine("[bold]VitaCernita CLI[/]");
        AnsiConsole.MarkupLine("Usage: dotnet run --project src/VitaCernita [COMMAND] [OPTIONS]\n");
        AnsiConsole.MarkupLine("[bold]Commands:[/]");
        AnsiConsole.MarkupLine("  run                     Execute the triage engine with Lua config (default)");
        AnsiConsole.MarkupLine("  validate                Validate the specified Lua config file");
        AnsiConsole.MarkupLine("  eval <code>             Execute an arbitrary Lua snippet\n");
        AnsiConsole.MarkupLine("[bold]Options:[/]");
        AnsiConsole.MarkupLine("  -c, --config <path>     Path to the Lua configuration file");
        AnsiConsole.MarkupLine("  -v, --version           Display application version");
        AnsiConsole.MarkupLine("  -h, --help              Show this help message");
    }
}
