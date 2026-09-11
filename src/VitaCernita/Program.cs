using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Spectre.Console;
using VitaCernita.Core.Api;
using VitaCernita.Core.Api.Auth;
using VitaCernita.Core.Api.Fakes;
using VitaCernita.Core.AutoReply.Diff;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Sources;

namespace VitaCernita.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string configPath = "config/gmail_filter.lua";
        bool explicitAnd = false;
        bool runDiff = false;
        bool useMock = false;
        string? token = null;
        string userId = "me";

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
                case "--diff":
                    runDiff = true;
                    break;
                case "--mock":
                case "--fake-account":
                    useMock = true;
                    break;
                case "--token":
                    if (i + 1 < args.Length) token = args[++i];
                    break;
                case "--user":
                    if (i + 1 < args.Length) userId = args[++i];
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
            var config = await loader.LoadConfigurationFromFileAsync(configPath);

            if (config.Labels.Count > 0)
            {
                AnsiConsole.MarkupLine($"[bold yellow]Configured Labels ({config.Labels.Count}):[/]\n");
                var table = new Table().Border(TableBorder.Rounded);
                table.AddColumn("[bold]Label Name[/]");
                table.AddColumn("[bold]ID[/]");
                table.AddColumn("[bold]Message List[/]");
                table.AddColumn("[bold]Label List[/]");
                table.AddColumn("[bold]Color (Text / Bg)[/]");

                foreach (var lbl in config.Labels)
                {
                    string colorInfo = lbl.Color != null
                        ? $"{lbl.Color.TextColor} / {lbl.Color.BackgroundColor}"
                        : "[dim]None[/]";

                    table.AddRow(
                        $"[cyan]{Markup.Escape(lbl.Name)}[/]",
                        lbl.Id != null ? Markup.Escape(lbl.Id) : "[dim]-[/]",
                        lbl.MessageListVisibility ?? "[dim]default[/]",
                        lbl.LabelListVisibility ?? "[dim]default[/]",
                        colorInfo
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }

            if (config.AutoReply != null)
            {
                var ar = config.AutoReply;
                AnsiConsole.MarkupLine("[bold yellow]Configured Auto-Reply (Vacation Responder):[/]\n");
                var arTable = new Table().Border(TableBorder.Rounded);
                arTable.AddColumn("[bold]Setting[/]");
                arTable.AddColumn("[bold]Configured Value[/]");

                arTable.AddRow("Status", ar.EnableAutoReply ? "[bold green]Enabled[/]" : "[bold red]Disabled[/]");
                arTable.AddRow("Response Subject", ar.ResponseSubject != null ? $"[cyan]{Markup.Escape(ar.ResponseSubject)}[/]" : "[dim]<none>[/]");
                if (!string.IsNullOrWhiteSpace(ar.ResponseBodyPlainText))
                {
                    string snippet = ar.ResponseBodyPlainText.Length > 60 ? ar.ResponseBodyPlainText[..57] + "..." : ar.ResponseBodyPlainText;
                    arTable.AddRow("Plain Text Body", Markup.Escape(snippet.Replace("\n", " ")));
                }
                if (!string.IsNullOrWhiteSpace(ar.ResponseBodyHtml))
                {
                    string snippet = ar.ResponseBodyHtml.Length > 60 ? ar.ResponseBodyHtml[..57] + "..." : ar.ResponseBodyHtml;
                    arTable.AddRow("HTML Body", Markup.Escape(snippet.Replace("\n", " ")));
                }
                arTable.AddRow("Restrict to Contacts", ar.RestrictToContacts ? "[yellow]True[/]" : "False");
                arTable.AddRow("Restrict to Domain", ar.RestrictToDomain ? "[yellow]True (Workspace Only)[/]" : "False");
                if (ar.StartTime.HasValue)
                {
                    arTable.AddRow("Start Time", $"{ar.StartDateTime:yyyy-MM-dd HH:mm:ss} UTC");
                }
                if (ar.EndTime.HasValue)
                {
                    arTable.AddRow("End Time", $"{ar.EndDateTime:yyyy-MM-dd HH:mm:ss} UTC");
                }

                AnsiConsole.Write(arTable);
                AnsiConsole.WriteLine();
            }

            var filters = config.Filters;
            if (filters.Count == 0 && config.Labels.Count == 0 && config.AutoReply == null)
            {
                AnsiConsole.MarkupLine("[bold yellow]No filters, labels, or auto-reply settings found in configuration.[/]");
                return 0;
            }

            if (runDiff)
            {
                IGmailSource currentSource;
                if (useMock)
                {
                    AnsiConsole.MarkupLine("[bold yellow]Mode:[/] In-Memory Fake Gmail Account (Mock)");
                    var fake = new FakeGmailApiClient();
                    fake.AddLabel(new GmailLabel("Receipts", id: "Label_1", messageListVisibility: "show", labelListVisibility: "labelShow"));
                    fake.AddLabel(new GmailLabel("OldUnusedTag", id: "Label_99"));
                    currentSource = new ApiGmailSource(fake, userId, name: "Fake Account (Mock)");
                }
                else
                {
                    var tokenProvider = new BearerTokenProvider(token);
                    var client = new HttpGmailApiClient(new HttpClient(), tokenProvider);
                    currentSource = new ApiGmailSource(client, userId);
                }

                var desiredSource = new LuaGmailSource(configPath);

                AnsiConsole.MarkupLine($"[bold cyan]Diffing '{Markup.Escape(currentSource.Name)}' against '{Markup.Escape(desiredSource.Name)}'...[/]\n");
                var labelDiff = await GmailSourceDiffer.DiffLabelsAsync(currentSource, desiredSource);
                AnsiConsole.WriteLine(labelDiff.ToDryRunReport());

                var autoReplyDiff = await GmailSourceDiffer.DiffAutoReplyAsync(
                    currentSource,
                    desiredSource,
                    new AutoReplyDiffOptions { TargetAccount = userId });

                if (autoReplyDiff.HasChanges || autoReplyDiff.AccountError != null || config.AutoReply != null)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.WriteLine(autoReplyDiff.ToDryRunReport());
                }

                return 0;
            }

            for (int index = 0; index < filters.Count; index++)
            {
                var filter = filters[index];
                string title = !string.IsNullOrWhiteSpace(filter.Name)
                    ? filter.Name
                    : $"Filter #{index + 1}";

                string canonicalQuery = filter.ToGmailQuery(explicitAnd: false);
                string explicitAndQuery = filter.ToGmailQuery(explicitAnd: true);

                string idInfo = !string.IsNullOrWhiteSpace(filter.Id)
                    ? $"[bold white]Filter ID:[/] [magenta]{Markup.Escape(filter.Id)}[/]\n"
                    : string.Empty;

                string actionInfo = filter.Action != null && !filter.Action.IsEmpty
                    ? $"\n\n[bold white]Action:[/] [cyan]{Markup.Escape(filter.Action.ToString())}[/]"
                    : string.Empty;

                var panel = new Panel(
                    new Markup(
                        idInfo +
                        $"[bold white]Search Query:[/] [bold green]{Markup.Escape(explicitAnd ? explicitAndQuery : canonicalQuery)}[/]\n\n" +
                        $"[dim]Canonical (Space-AND):[/] [yellow]{Markup.Escape(canonicalQuery)}[/]\n" +
                        $"[dim]Explicit AND Keyword :[/] [yellow]{Markup.Escape(explicitAndQuery)}[/]" +
                        actionInfo
                    ))
                {
                    Header = new PanelHeader($"[bold cyan]{Markup.Escape(title)}[/]"),
                    Border = BoxBorder.Rounded,
                    Padding = new Padding(2, 1)
                };

                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();

                // Directly output the exact search text for easy copying/piping
                AnsiConsole.MarkupLine("[bold]Precise Gmail Query Text:[/] [green]" + Markup.Escape(canonicalQuery) + "[/]");
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
        AnsiConsole.MarkupLine("[bold]VitaCernita CLI - Gmail Filter, Label & Auto-Reply Manager[/]");
        AnsiConsole.MarkupLine("Usage: dotnet run --project src/VitaCernita -- [[OPTIONS]]\n");
        AnsiConsole.MarkupLine("[bold]Options:[/]");
        AnsiConsole.MarkupLine("  -c, --config <path>     Path to the Lua configuration file (default: config/gmail_filter.lua)");
        AnsiConsole.MarkupLine("      --diff              Diff local Lua configuration (labels & auto-reply) against target Gmail account");
        AnsiConsole.MarkupLine("      --mock              Use in-memory fake Gmail API client for dry-run testing");
        AnsiConsole.MarkupLine("      --token <token>     Bearer token for Gmail API (defaults to GMAIL_ACCESS_TOKEN)");
        AnsiConsole.MarkupLine("      --user <userId>     Target Gmail user ID (default: 'me')");
        AnsiConsole.MarkupLine("      --explicit-and      Render queries using the explicit 'AND' keyword");
        AnsiConsole.MarkupLine("  -v, --version           Display application version");
        AnsiConsole.MarkupLine("  -h, --help              Show this help message");
    }
}
