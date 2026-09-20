using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using VitaCernita.Core.Configuration;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// Clears cached OAuth 2.0 tokens and optionally client credentials from the configuration directory.
/// </summary>
[Command("logout", Description = "Log out and remove cached Google OAuth tokens")]
public class LogoutCommand : ICliCommand
{
    private readonly IAnsiConsole _console;

    public LogoutCommand(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public Task<int> ExecuteAsync(string[] args)
    {
        string? account = null;
        string? userId = null;
        string? explicitConfigDir = null;
        bool all = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
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
                case "--all":
                    all = true;
                    break;
                case "-h":
                case "--help":
                    PrintHelp();
                    return Task.FromResult(0);
            }
        }

        string configDir = ConfigPathResolver.GetDefaultConfigDirectory(customConfigDir: explicitConfigDir);
        string tokenDir = ConfigPathResolver.GetTokenStorageDirectory(configDir);
        string targetUser = !string.IsNullOrWhiteSpace(userId)
            ? userId
            : (!string.IsNullOrWhiteSpace(account) ? account : string.Empty);

        int deletedCount = 0;

        if (Directory.Exists(tokenDir))
        {
            var files = Directory.EnumerateFiles(tokenDir).ToList();
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                if (string.IsNullOrEmpty(targetUser) || fileName.Contains(targetUser, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch
                    {
                        // Best effort deletion
                    }
                }
            }
        }

        if (all)
        {
            string credPath = ConfigPathResolver.GetCredentialsPath(configDir);
            if (File.Exists(credPath))
            {
                try
                {
                    File.Delete(credPath);
                    _console.MarkupLine($"Removed client credentials at: [yellow]{Markup.Escape(credPath)}[/]");
                }
                catch
                {
                    // Best effort
                }
            }
        }

        if (deletedCount > 0)
        {
            string targetDesc = !string.IsNullOrEmpty(targetUser) ? $"for user '[cyan]{Markup.Escape(targetUser)}[/]'" : "for all accounts";
            _console.MarkupLine($"[bold green]Successfully logged out[/] {targetDesc}. Removed {deletedCount} token file(s).");
        }
        else
        {
            _console.MarkupLine("[bold yellow]No cached tokens found to remove.[/]");
        }

        return Task.FromResult(0);
    }

    public void PrintHelp()
    {
        _console.MarkupLine("[bold]VitaCernita CLI - Logout Command[/]");
        _console.MarkupLine("Usage: vitacernita logout [[OPTIONS]]\n");
        _console.MarkupLine("[bold]Description:[/]");
        _console.MarkupLine("  Remove cached Google OAuth 2.0 tokens from the configuration directory.\n");
        _console.MarkupLine("[bold]Options:[/]");
        _console.MarkupLine("  -a, --account <email>    Specific account email to log out");
        _console.MarkupLine("  -u, --user <userId>      Specific user ID to log out (defaults to all)");
        _console.MarkupLine("      --config-dir <dir>   Custom configuration directory (default: ~/.config/vitacernita)");
        _console.MarkupLine("      --all                Also remove cached client credentials (credentials.json)");
        _console.MarkupLine("  -h, --help               Show this help message");
    }
}
