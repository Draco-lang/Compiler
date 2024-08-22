using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using PrettyPrompt.Consoles;
using static Basic.Reference.Assemblies.Net80;

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
