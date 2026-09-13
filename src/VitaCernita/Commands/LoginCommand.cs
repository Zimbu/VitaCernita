using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console;
using VitaCernita.Core.Api.Auth;
using VitaCernita.Core.Configuration;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// Authenticates the user with Google OAuth 2.0 and securely caches tokens in the configuration directory.
/// </summary>
public class LoginCommand : ICliCommand
{
    private readonly IAnsiConsole _console;
    private readonly GoogleOAuthTokenProvider? _tokenProviderOverride;

    public LoginCommand(IAnsiConsole? console = null, GoogleOAuthTokenProvider? tokenProviderOverride = null)
    {
        _console = console ?? AnsiConsole.Console;
        _tokenProviderOverride = tokenProviderOverride;
    }

    public string Name => "login";
    public string Description => "Authenticate with Google OAuth 2.0 to access the Gmail API";
    public IReadOnlyList<string> Aliases => Array.Empty<string>();

    public async Task<int> ExecuteAsync(string[] args)
    {
        string? explicitCredentialsFile = null;
        string? clientId = null;
        string? clientSecret = null;
        string? account = null;
        string? userId = null;
        string? explicitConfigDir = null;
        bool saveCredentialsOnly = false;
        bool force = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-c":
                case "--credentials":
                    if (i + 1 < args.Length) explicitCredentialsFile = args[++i];
                    break;
                case "--client-id":
                    if (i + 1 < args.Length) clientId = args[++i];
                    break;
                case "--client-secret":
                    if (i + 1 < args.Length) clientSecret = args[++i];
                    break;
                case "-a":
                case "--account":
                    if (i + 1 < args.Length) account = args[++i];
                    break;
                case "-u":
                case "--user":
                    if (i + 1 < args.Length) userId = args[++i];
                    break;
                case "--config-dir":
                    if (i + 1 < args.Length) explicitConfigDir = args[++i];
                    break;
                case "--save-only":
                case "--credentials-only":
                    saveCredentialsOnly = true;
                    break;
                case "-f":
                case "--force":
                    force = true;
                    break;
                case "-h":
                case "--help":
                    PrintHelp();
                    return 0;
            }
        }

        string configDir = ConfigPathResolver.GetDefaultConfigDirectory(customConfigDir: explicitConfigDir);
        string effectiveUser = !string.IsNullOrWhiteSpace(userId)
            ? userId
            : (!string.IsNullOrWhiteSpace(account) ? account : "user");

        // 1. Resolve client credentials
        ClientCredentials? credentials = null;

        if (!string.IsNullOrWhiteSpace(explicitCredentialsFile))
        {
            credentials = await ClientCredentialsManager.LoadCredentialsAsync(explicitCredentialsFile, configDir);
            if (credentials == null || !credentials.IsValid)
            {
                _console.MarkupLine($"[bold red]Error:[/] Could not parse valid client credentials from '[yellow]{Markup.Escape(explicitCredentialsFile)}[/]'.");
                return 1;
            }
            await ClientCredentialsManager.SaveCredentialsAsync(credentials, configDir: configDir);
            _console.MarkupLine($"[bold green]Imported credentials from:[/] [cyan]{Markup.Escape(explicitCredentialsFile)}[/]");
        }
        else if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
        {
            credentials = new ClientCredentials(clientId.Trim(), clientSecret.Trim());
            await ClientCredentialsManager.SaveCredentialsAsync(credentials, configDir: configDir);
            _console.MarkupLine("[bold green]Client credentials saved successfully.[/]");
        }
        else
        {
            credentials = await ClientCredentialsManager.LoadCredentialsAsync(configDir: configDir);
        }

        // 2. If credentials still not found, prompt interactively if possible
        if (credentials == null || !credentials.IsValid)
        {
            if (Console.IsInputRedirected)
            {
                _console.MarkupLine("[bold red]Error:[/] Google OAuth Client credentials not found.");
                _console.MarkupLine("Provide [bold]--credentials <file.json>[/] or [bold]--client-id[/] and [bold]--client-secret[/].");
                _console.MarkupLine("See docs/auth/README.md for setup instructions.");
                return 1;
            }

            _console.MarkupLine("[bold yellow]Google OAuth Client credentials not found.[/]");
            _console.MarkupLine("Please enter your OAuth Client ID and Secret (or run with --credentials <file.json>):\n");

            string inputId = _console.Prompt(new TextPrompt<string>("Enter [cyan]OAuth Client ID[/]:").PromptStyle("green"));
            string inputSecret = _console.Prompt(new TextPrompt<string>("Enter [cyan]OAuth Client Secret[/]:").PromptStyle("green").Secret());

            if (string.IsNullOrWhiteSpace(inputId) || string.IsNullOrWhiteSpace(inputSecret))
            {
                _console.MarkupLine("[bold red]Error:[/] Client ID and Client Secret cannot be empty.");
                return 1;
            }

            credentials = new ClientCredentials(inputId.Trim(), inputSecret.Trim());
            await ClientCredentialsManager.SaveCredentialsAsync(credentials, configDir: configDir);
            _console.MarkupLine($"[bold green]Client credentials saved to:[/] [yellow]{Markup.Escape(ConfigPathResolver.GetCredentialsPath(configDir))}[/]\n");
        }

        if (saveCredentialsOnly)
        {
            _console.MarkupLine("[bold green]Client credentials saved successfully. Skipping interactive login as requested.[/]");
            return 0;
        }

        // 3. Check for existing cached token
        var provider = _tokenProviderOverride ?? new GoogleOAuthTokenProvider(
            credentials: credentials,
            configDir: configDir,
            user: effectiveUser);

        if (!force && provider.HasCachedToken())
        {
            try
            {
                string? existingToken = await provider.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(existingToken))
                {
                    _console.MarkupLine($"[bold green]Already authenticated as [cyan]{Markup.Escape(effectiveUser)}[/].[/]");
                    _console.MarkupLine($"Token storage: [yellow]{Markup.Escape(ConfigPathResolver.GetTokenStorageDirectory(configDir))}[/]");
                    _console.MarkupLine("[dim]Use --force to re-authenticate or switch accounts.[/]");
                    return 0;
                }
            }
            catch
            {
                // Expired or invalid, proceed to re-auth
            }
        }

        // Never open an interactive browser in automated test runners unless explicitly provided a mock
        if (ConfigPathResolver.IsTestEnvironment && _tokenProviderOverride == null)
        {
            _console.MarkupLine("[dim]Test environment detected: skipping interactive browser launch.[/]");
            return 0;
        }

        // 4. Run authorization flow
        _console.MarkupLine($"Initiating OAuth 2.0 authorization for [cyan]{Markup.Escape(effectiveUser)}[/]...");
        _console.MarkupLine("[dim]Opening browser window for Google authentication. Complete the sign-in prompt to continue...[/]\n");

        try
        {
            var credential = await provider.GetUserCredentialAsync();
            string? token = await credential.GetAccessTokenForRequestAsync();

            if (!string.IsNullOrEmpty(token))
            {
                _console.MarkupLine($"[bold green]Successfully authenticated as [cyan]{Markup.Escape(effectiveUser)}[/]![/]");
                _console.MarkupLine($"Tokens securely cached at: [yellow]{Markup.Escape(ConfigPathResolver.GetTokenStorageDirectory(configDir))}[/]");
                return 0;
            }

            _console.MarkupLine("[bold red]Error:[/] Failed to obtain access token from Google.");
            return 1;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[bold red]Authentication failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    public void PrintHelp()
    {
        _console.MarkupLine("[bold]VitaCernita CLI - Login Command[/]");
        _console.MarkupLine("Usage: vitacernita login [[OPTIONS]]\n");
        _console.MarkupLine("[bold]Description:[/]");
        _console.MarkupLine("  Authenticate with Google OAuth 2.0 using the loopback browser flow.");
        _console.MarkupLine("  Saves client credentials and securely caches access/refresh tokens for future commands.\n");
        _console.MarkupLine("[bold]Options:[/]");
        _console.MarkupLine("  -c, --credentials <file> Path to Google Cloud client_secret_xxx.json");
        _console.MarkupLine("      --client-id <id>     Google OAuth Client ID");
        _console.MarkupLine("      --client-secret <sec> Google OAuth Client Secret");
        _console.MarkupLine("  -a, --account <email>    Target Gmail account email address");
        _console.MarkupLine("  -u, --user <userId>      Target user ID for token storage (default: 'user')");
        _console.MarkupLine("      --config-dir <dir>   Custom configuration directory (default: ~/.config/vitacernita)");
        _console.MarkupLine("  -f, --force              Force re-authentication even if valid cached token exists");
        _console.MarkupLine("  -h, --help               Show this help message");
    }
}
