using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using static Basic.Reference.Assemblies.Net80;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;

namespace Draco.Repl;

internal static class Program
{
    // TODO: Temporary until we find out how we can inherit everything from the host
    private static IEnumerable<MetadataReference> BclReferences => ReferenceInfos.All
        .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)));

    internal static void Main(string[] args)
    {
        var session = new ReplSession([.. BclReferences]);

        // val x = System.Random.Shared.Next();
        // PrevModulesName.x
        // PrevModulesName.x

        var result1 = session.Evaluate(
            VariableDeclaration(
                "x",
                null,
                CallExpression(MemberExpression(MemberExpression(MemberExpression(NameExpression("System"), "Random"), "Shared"), "Next"))));
        PrintResult(result1);

        var result2 = session.Evaluate(
            BinaryExpression(MemberExpression(NameExpression("Context0"), "x"), Plus, LiteralExpression(2)));
        PrintResult(result2);

        var result3 = session.Evaluate(
            BinaryExpression(MemberExpression(NameExpression("Context0"), "x"), Plus, LiteralExpression(2)));
        PrintResult(result3);
    }

    private static void PrintResult(ReplResult result)
    {
        if (result.Success)
        {
            Console.WriteLine(result.Value);
        }
        else
        {
            Console.WriteLine("Errors:");
            foreach (var diagnostic in result.Diagnostics) Console.WriteLine(diagnostic);
        }
    }
}
