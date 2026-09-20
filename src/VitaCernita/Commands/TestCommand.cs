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
using VitaCernita.Core.Diffing.AutoReply;
using VitaCernita.Core.Diffing.Filters;
using VitaCernita.Core.Diffing.Labels;
using VitaCernita.Core.Configuration;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Queries;
using VitaCernita.Core.Reporting;
using VitaCernita.Core.Sources;
using VitaCernita.Core.Sync;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// CLI command that displays configured labels, auto-reply, and filters or diffs against a target Gmail account.
/// This represents the original test execution pipeline and will be gradually phased out.
/// </summary>
[Command("test", Description = "Display configured labels, auto-reply, and filters or diff against target Gmail account (test mode)", Aliases = ["t"])]
public class TestCommand : ICliCommand
{
    private readonly IAnsiConsole _console;

    public TestCommand(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    [Option("config", 'c', Description = "Path to the Lua configuration file (default: ~/.config/vitacernita/gmail.lua)", ValueHelp = "<path>")]
    public string? ConfigPath { get; set; }

    [Option("config-dir", Description = "Custom configuration directory (default: ~/.config/vitacernita)", ValueHelp = "<dir>")]
    public string? ConfigDir { get; set; }

    [Option("explicit-and", Description = "Render queries using the explicit 'AND' keyword")]
    public bool ExplicitAnd { get; set; }

    [Option("diff", Aliases = ["dry-run", "sync-plan"], Description = "Diff local configuration against target Gmail account and output sync plan")]
    public bool RunDiff { get; set; }

    [Option("mock", Aliases = ["fake-account"], Description = "Use in-memory fake Gmail API client for dry-run testing")]
    public bool UseMock { get; set; }

    [Option("token", Description = "Bearer token for Gmail API (defaults to GMAIL_ACCESS_TOKEN)", ValueHelp = "<token>")]
    public string? Token { get; set; }

    [Option("user", Description = "Target Gmail user ID (default: 'me')", ValueHelp = "<userId>")]
    public string UserId { get; set; } = "me";

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length > 0)
        {
            var bindResult = Binding.CommandParameterBinder.Default.Bind(this, args);
            if (bindResult.HelpRequested)
            {
                PrintHelp();
                return 0;
            }
            if (!bindResult.IsSuccess)
            {
                _console.MarkupLine($"[bold red]Error:[/] {bindResult.ErrorMessage}");
                return 1;
            }
        }

        // Header banner
        _console.Write(
            new FigletText("VitaCernita")
                .LeftJustified()
                .Color(Color.Cyan1));

        _console.MarkupLine("[bold]Gmail Filter Configuration Engine (Lua DSL & C# Core)[/]\n");

        string configDir = ConfigPathResolver.GetDefaultConfigDirectory(customConfigDir: ConfigDir);
        string configPath = ConfigPathResolver.ResolveConfigPath(ConfigPath, customConfigDir: ConfigDir);

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

            if (RunDiff)
            {
                IGmailSource currentSource;
                if (UseMock)
                {
                    _console.MarkupLine("[bold yellow]Mode:[/] In-Memory Fake Gmail Account (Mock)");
                    var fake = new FakeGmailApiClient();
                    fake.AddLabel(new GmailLabel("Receipts", id: "Label_1", messageListVisibility: "show", labelListVisibility: "labelShow"));
                    fake.AddLabel(new GmailLabel("OldUnusedTag", id: "Label_99"));
                    fake.AddFilter(new GmailFilter("sec-001", new FieldCondition("from", "secops@company.com"), new GmailAction().Star()));
                    fake.AddFilter(new GmailFilter("old-vendor-009", new FieldCondition("from", "spam@vendor.com"), new GmailAction().Delete()));
                    currentSource = new ApiGmailSource(fake, UserId, name: "Fake Account (Mock)");
                }
                else
                {
                    IGmailTokenProvider tokenProvider;

                    if (!string.IsNullOrWhiteSpace(Token))
                    {
                        if (Token.Contains(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase))
                        {
                            _console.MarkupLine("[bold red]Error:[/] The provided --token appears to be an OAuth Client ID, not an OAuth 2.0 access token.");
                            _console.MarkupLine("OAuth 2.0 access tokens typically begin with 'ya29.' or are retrieved via [bold cyan]vitacernita login[/].");
                            return 1;
                        }
                        tokenProvider = new BearerTokenProvider(Token);
                    }
                    else
                    {
                        var oauthProvider = new GoogleOAuthTokenProvider(configDir: configDir, user: UserId);
                        if (oauthProvider.HasCachedToken())
                        {
                            _console.MarkupLine($"Using cached Google OAuth token for [cyan]{Markup.Escape(UserId)}[/]...");
                            string? envToken = Environment.GetEnvironmentVariable("GMAIL_ACCESS_TOKEN");
                            if (!string.IsNullOrWhiteSpace(envToken) && envToken.Contains(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase))
                            {
                                _console.MarkupLine("[grey](Note: Ignoring GMAIL_ACCESS_TOKEN environment variable because it contains an OAuth Client ID instead of an access token.)[/]");
                            }
                            tokenProvider = oauthProvider;
                        }
                        else
                        {
                            string? envToken = Environment.GetEnvironmentVariable("GMAIL_ACCESS_TOKEN");
                            if (!string.IsNullOrWhiteSpace(envToken))
                            {
                                if (envToken.Contains(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase))
                                {
                                    _console.MarkupLine("[bold red]Error:[/] The GMAIL_ACCESS_TOKEN environment variable is set to an OAuth Client ID ('...apps.googleusercontent.com'), not an OAuth 2.0 access token.");
                                    _console.MarkupLine("OAuth 2.0 access tokens typically begin with 'ya29.'. Run [bold cyan]unset GMAIL_ACCESS_TOKEN[/] in your terminal and authenticate using [bold cyan]vitacernita login[/].");
                                    return 1;
                                }

                                tokenProvider = new BearerTokenProvider(envToken);
                            }
                            else
                            {
                                _console.MarkupLine($"[bold red]Error:[/] No active Google login session or access token found for account '[yellow]{Markup.Escape(UserId)}[/]'.");
                                _console.MarkupLine("Run [bold cyan]vitacernita login[/] to authenticate, or provide [bold]--token <token>[/].");
                                return 1;
                            }
                        }
                    }

                    var client = new HttpGmailApiClient(new HttpClient(), tokenProvider);
                    currentSource = new ApiGmailSource(client, userId: "me", name: $"Gmail Account ({UserId})");
                }

                var desiredSource = new LuaGmailSource(configPath);

                _console.MarkupLine($"[bold cyan]Diffing '{Markup.Escape(currentSource.Name)}' against '{Markup.Escape(desiredSource.Name)}'...[/]\n");
                var labelDiff = await GmailSourceDiffer.DiffLabelsAsync(currentSource, desiredSource);
                var filterDiff = await GmailSourceDiffer.DiffFiltersAsync(currentSource, desiredSource);
                var autoReplyDiff = await GmailSourceDiffer.DiffAutoReplyAsync(
                    currentSource,
                    desiredSource,
                    new AutoReplyDiffOptions { TargetAccount = UserId });

                var syncPlan = GmailSyncPlanner.BuildPlan(labelDiff, filterDiff, autoReplyDiff);
                var report = DryRunReportGenerator.CreateReport(labelDiff, filterDiff, autoReplyDiff, syncPlan);
                _console.WriteLine(report);

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
                        $"[bold white]Search Query:[/] [bold green]{Markup.Escape(ExplicitAnd ? explicitAndQuery : canonicalQuery)}[/]\n\n" +
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

    public void PrintHelp() => Binding.CommandHelpRenderer.Render(this, _console);
}
