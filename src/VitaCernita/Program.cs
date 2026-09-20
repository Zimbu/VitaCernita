using System.Threading.Tasks;
using VitaCernita.Cli.Commands;
using VitaCernita.Cli.Engine;

namespace VitaCernita.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var dispatcher = CreateDefaultDispatcher();
        return await dispatcher.DispatchAsync(args);
    }

    public static CommandDispatcher CreateDefaultDispatcher()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.RegisterFromAssembly(typeof(Program).Assembly);
        return dispatcher;
    }
}
