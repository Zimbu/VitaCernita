namespace VitaCernita.Cli.Engine;

/// <summary>
/// Interface implemented by commands that require a reference to the <see cref="CommandDispatcher"/>.
/// </summary>
public interface IDispatcherAware
{
    /// <summary>
    /// Injects the active <see cref="CommandDispatcher"/> instance.
    /// </summary>
    /// <param name="dispatcher">The dispatcher managing this command.</param>
    void SetDispatcher(CommandDispatcher dispatcher);
}
