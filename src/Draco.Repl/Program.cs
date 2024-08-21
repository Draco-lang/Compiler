using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using static Basic.Reference.Assemblies.Net80;

namespace Draco.Repl;

internal static class Program
{
    // TODO: Temporary until we find out how we can inherit everything from the host
    private static IEnumerable<MetadataReference> BclReferences => ReferenceInfos.All
        .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)));

    internal static void Main(string[] args)
    {
        var session = new ReplSession([.. BclReferences]);
        session.AddImports(
            "System",
            "System.Collections.Generic",
            "System.Linq");

        while (true)
        {
            Console.Write("> ");
            var result = session.Evaluate(Console.In);
            PrintResult(result);
        }
    }

    private static void PrintResult(ExecutionResult<object?> result)
    {
        if (result.Success)
        {
            Console.WriteLine(result.Value);
        }
        else
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            foreach (var diagnostic in result.Diagnostics) Console.WriteLine(diagnostic);

            Console.ForegroundColor = oldColor;
        }
    }
}
