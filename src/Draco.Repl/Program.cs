using System.Threading.Tasks;
using PrettyPrompt.Consoles;

namespace Draco.Repl;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        var configuration = new Configuration();
        var console = new SystemConsole();

        var loop = new Loop(configuration, console);
        await loop.Run();
    }
}
