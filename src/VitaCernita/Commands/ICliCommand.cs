using System.Collections.Generic;
using System.Threading.Tasks;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// Represents a runnable CLI subcommand.
/// </summary>
public interface ICliCommand
{
    /// <summary>
    /// The primary name used to invoke this command (e.g. "test", "initialize").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Brief description of the command for CLI help output.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Alternate names or abbreviations for this command (e.g. "init" for "initialize").
    /// </summary>
    IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// Executes the command with the provided arguments.
    /// </summary>
    /// <param name="args">Command-specific arguments (excluding the command name itself).</param>
    /// <returns>Exit code (0 for success, non-zero for failure).</returns>
    Task<int> ExecuteAsync(string[] args);

    /// <summary>
    /// Displays help details and usage flags for this specific command.
    /// </summary>
    void PrintHelp();
}
