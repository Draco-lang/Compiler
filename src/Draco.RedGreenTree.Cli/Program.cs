using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace Draco.RedGreenTree.Cli;

[Verb("green", HelpText = "Generate boilerplate implementations for green nodes.")]
internal sealed class GreenTreeOptions
{
    [Option('p', "project", Required = true, HelpText = "The project to read types from.")]
    public string Project { get; set; } = string.Empty;

    [Option('t', "tree-root", Required = true, HelpText = "The fully qualified name of the green tree root type.")]
    public string TreeRoot { get; set; } = string.Empty;
}

[Verb("red", HelpText = "Generate boilerplate implementations for red nodes.")]
internal sealed class RedTreeOptions
{
    [Option('p', "project", Required = true, HelpText = "The project to read types from.")]
    public string Project { get; set; } = string.Empty;

    [Option('t', "tree-root", Required = true, HelpText = "The fully qualified name of the green tree root type.")]
    public string TreeRoot { get; set; } = string.Empty;
}

[Verb("visitor", HelpText = "Generate visitor for the internal tree.")]
internal sealed class VisitorOptions
{
    [Option('p', "project", Required = true, HelpText = "The project to read types from.")]
    public string Project { get; set; } = string.Empty;

    [Option('t', "tree-root", Required = true, HelpText = "The fully qualified name of the green tree root type.")]
    public string TreeRoot { get; set; } = string.Empty;
}

internal class Program
{
    private static async Task<INamedTypeSymbol?> LoadTypeFromProject(string projectPath, string typePath)
    {
        MSBuildLocator.RegisterDefaults();
        var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);
        var compilation = await project.GetCompilationAsync();
        return compilation?.GetTypeByMetadataName(typePath);
    }

    internal static async Task Main(string[] args) => await Parser.Default
        .ParseArguments<
            GreenTreeOptions,
            RedTreeOptions,
            VisitorOptions>(args)
        .MapResult(
            async (GreenTreeOptions opts) =>
            {
                var type = await LoadTypeFromProject(opts.Project, opts.TreeRoot);
                if (type is null)
                {
                    Console.Error.WriteLine($"Could not load {opts.TreeRoot} from {opts.Project}");
                    return 1;
                }
                var code = GreenTreeGenerator.Generate(type);
                Console.WriteLine(code);
                return 0;
            },
            async (RedTreeOptions opts) =>
            {
                var type = await LoadTypeFromProject(opts.Project, opts.TreeRoot);
                if (type is null)
                {
                    Console.Error.WriteLine($"Could not load {opts.TreeRoot} from {opts.Project}");
                    return 1;
                }
                var code = RedTreeGenerator.Generate(type);
                Console.WriteLine(code);
                return 0;
            },
            async (VisitorOptions opts) =>
            {
                var type = await LoadTypeFromProject(opts.Project, opts.TreeRoot);
                if (type is null)
                {
                    Console.Error.WriteLine($"Could not load {opts.TreeRoot} from {opts.Project}");
                    return 1;
                }
                var code = VisitorGenerator.Generate(type);
                Console.WriteLine(code);
                return 0;
            },
            errs =>
            {
                foreach (var err in errs) Console.Error.WriteLine(err);
                return Task.FromResult(1);
            });
}
