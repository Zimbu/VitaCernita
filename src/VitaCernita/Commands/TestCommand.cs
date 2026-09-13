using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Spectre.Console;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Api;
using VitaCernita.Core.Api.Auth;
using VitaCernita.Core.Api.Fakes;
using VitaCernita.Core.AutoReply.Diff;
using VitaCernita.Core.Configuration;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Filters.Diff;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Queries;
using VitaCernita.Core.Sources;
using VitaCernita.Core.Sync;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// CLI command that displays configured labels, auto-reply, and filters or diffs against a target Gmail account.
/// This represents the original test execution pipeline and will be gradually phased out.
/// </summary>
public class TestCommand : ICliCommand
{
    private readonly IAnsiConsole _console;

    public TestCommand(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public string Name => "test";
    public string Description => "Display configured labels, auto-reply, and filters or diff against target Gmail account (legacy test mode)";
    public IReadOnlyList<string> Aliases => Array.Empty<string>();

    public async Task<int> ExecuteAsync(string[] args)
    {
        string? explicitConfigPath = null;
        string? explicitConfigDir = null;
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
                    if (i + 1 < args.Length) explicitConfigPath = args[++i];
                    break;
                case "--config-dir":
                    if (i + 1 < args.Length) explicitConfigDir = args[++i];
                    break;
                case "--explicit-and":
                    explicitAnd = true;
                    break;
                case "--diff":
                case "--dry-run":
                case "--sync-plan":
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
                case "-h":
                case "--help":
                    PrintHelp();
                    return 0;
            }
        }

        // Header banner
        _console.Write(
            new FigletText("VitaCernita")
                .LeftJustified()
                .Color(Color.Cyan1));

        _console.MarkupLine("[bold]Gmail Filter Configuration Engine (Lua DSL & C# Core)[/]\n");

        string configDir = ConfigPathResolver.GetDefaultConfigDirectory(customConfigDir: explicitConfigDir);
        string configPath = ConfigPathResolver.ResolveConfigPath(explicitConfigPath, customConfigDir: explicitConfigDir);

        if (!File.Exists(configPath))
        {
            _console.MarkupLine($"[bold red]Error:[/] Configuration file '[yellow]{Markup.Escape(configPath)}[/]' not found.");
            return 1;
        }

        _console.MarkupLine($"Loading configuration from: [cyan]{Markup.Escape(configPath)}[/]\n");

        var loader = new GmailFilterLoader();
        try
        {
            var config = await loader.LoadConfigurationFromFileAsync(configPath);

            if (config.Labels.Count > 0)
            {
                _console.MarkupLine($"[bold yellow]Configured Labels ({config.Labels.Count}):[/]\n");
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

                _console.Write(table);
                _console.WriteLine();
            }

            if (config.AutoReply != null)
            {
                var ar = config.AutoReply;
                _console.MarkupLine("[bold yellow]Configured Auto-Reply (Vacation Responder):[/]\n");
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

                _console.Write(arTable);
                _console.WriteLine();
            }

            var filters = config.Filters;
            if (filters.Count == 0 && config.Labels.Count == 0 && config.AutoReply == null)
            {
                _console.MarkupLine("[bold yellow]No filters, labels, or auto-reply settings found in configuration.[/]");
                return 0;
            }

            if (runDiff)
            {
                IGmailSource currentSource;
                if (useMock)
                {
                    _console.MarkupLine("[bold yellow]Mode:[/] In-Memory Fake Gmail Account (Mock)");
                    var fake = new FakeGmailApiClient();
                    fake.AddLabel(new GmailLabel("Receipts", id: "Label_1", messageListVisibility: "show", labelListVisibility: "labelShow"));
                    fake.AddLabel(new GmailLabel("OldUnusedTag", id: "Label_99"));
                    fake.AddFilter(new GmailFilter("sec-001", new FieldCondition("from", "secops@company.com"), new GmailAction().Star()));
                    fake.AddFilter(new GmailFilter("old-vendor-009", new FieldCondition("from", "spam@vendor.com"), new GmailAction().Delete()));
                    currentSource = new ApiGmailSource(fake, userId, name: "Fake Account (Mock)");
                }
                else
                {
                    token ??= Environment.GetEnvironmentVariable("GMAIL_ACCESS_TOKEN");
                    IGmailTokenProvider tokenProvider;

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        tokenProvider = new BearerTokenProvider(token);
                    }
                    else
                    {
                        var oauthProvider = new GoogleOAuthTokenProvider(configDir: configDir, user: userId);
                        if (oauthProvider.HasCachedToken())
                        {
                            _console.MarkupLine($"Using cached Google OAuth token for [cyan]{Markup.Escape(userId)}[/]...");
                            tokenProvider = oauthProvider;
                        }
                        else
                        {
                            _console.MarkupLine($"[bold red]Error:[/] No active Google login session or access token found for account '[yellow]{Markup.Escape(userId)}[/]'.");
                            _console.MarkupLine("Run [bold cyan]vitacernita login[/] to authenticate, or provide [bold]--token <token>[/].");
                            return 1;
                        }
                    }

                    var client = new HttpGmailApiClient(new HttpClient(), tokenProvider);
                    currentSource = new ApiGmailSource(client, userId);
                }

                var desiredSource = new LuaGmailSource(configPath);

                _console.MarkupLine($"[bold cyan]Diffing '{Markup.Escape(currentSource.Name)}' against '{Markup.Escape(desiredSource.Name)}'...[/]\n");
                var labelDiff = await GmailSourceDiffer.DiffLabelsAsync(currentSource, desiredSource);
                _console.WriteLine(labelDiff.ToDryRunReport());

                var filterDiff = await GmailSourceDiffer.DiffFiltersAsync(currentSource, desiredSource);
                _console.WriteLine();
                _console.WriteLine(filterDiff.ToDryRunReport());

                var autoReplyDiff = await GmailSourceDiffer.DiffAutoReplyAsync(
                    currentSource,
                    desiredSource,
                    new AutoReplyDiffOptions { TargetAccount = userId });

                if (autoReplyDiff.HasChanges || autoReplyDiff.AccountError != null || config.AutoReply != null)
                {
                    _console.WriteLine();
                    _console.WriteLine(autoReplyDiff.ToDryRunReport());
                }

                var syncPlan = GmailSyncPlanner.BuildPlan(labelDiff, filterDiff, autoReplyDiff);
                _console.WriteLine();
                _console.WriteLine(syncPlan.ToDryRunReport());

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

                _console.Write(panel);
                _console.WriteLine();

                _console.MarkupLine("[bold]Precise Gmail Query Text:[/] [green]" + Markup.Escape(canonicalQuery) + "[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[bold red]Error loading filter configuration:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    public void PrintHelp()
    {
        _console.MarkupLine("[bold]VitaCernita CLI - Test Command[/]");
        _console.MarkupLine("Usage: vitacernita test [[OPTIONS]]\n");
        _console.MarkupLine("[bold]Description:[/]");
        _console.MarkupLine("  Display configured labels, auto-reply, and filters or diff against target Gmail account (legacy mode).\n");
        _console.MarkupLine("[bold]Options:[/]");
        _console.MarkupLine("  -c, --config <path>     Path to the Lua configuration file (default: ~/.config/vitacernita/gmail.lua)");
        _console.MarkupLine("      --config-dir <dir>  Custom configuration directory (default: ~/.config/vitacernita)");
        _console.MarkupLine("      --diff              Diff local configuration (labels, filters, auto-reply) against target Gmail account");
        _console.MarkupLine("      --dry-run           Alias for --diff; computes diffs and outputs the dry-run synchronization plan");
        _console.MarkupLine("      --sync-plan         Generate and display the ordered sync commands to make target match configuration");
        _console.MarkupLine("      --mock              Use in-memory fake Gmail API client for dry-run testing");
        _console.MarkupLine("      --token <token>     Bearer token for Gmail API (defaults to GMAIL_ACCESS_TOKEN)");
        _console.MarkupLine("      --user <userId>     Target Gmail user ID (default: 'me')");
        _console.MarkupLine("      --explicit-and      Render queries using the explicit 'AND' keyword");
        _console.MarkupLine("  -h, --help              Show this help message");
    }
}
