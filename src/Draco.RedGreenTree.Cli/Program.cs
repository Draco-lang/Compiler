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

[Verb("visitor-interface", HelpText = "Generate visitor interface for the internal tree.")]
internal sealed class VisitorInterfaceOptions
{
    [Option('p', "project", Required = true, HelpText = "The project to read types from.")]
    public string Project { get; set; } = string.Empty;

    [Option('t', "tree-root", Required = true, HelpText = "The fully qualified name of the green tree root type.")]
    public string TreeRoot { get; set; } = string.Empty;

    [Option('v', "visitor", Required = true, HelpText = "The fully qualified name of the visitor type to base the generation on.")]
    public string Visitor { get; set; } = string.Empty;
}

[Verb("visitor-base", HelpText = "Generate visitor base for the internal tree.")]
internal sealed class VisitorBaseOptions
{
    [Option('p', "project", Required = true, HelpText = "The project to read types from.")]
    public string Project { get; set; } = string.Empty;

    [Option('t', "tree-root", Required = true, HelpText = "The fully qualified name of the green tree root type.")]
    public string TreeRoot { get; set; } = string.Empty;

    [Option('v', "visitor", Required = true, HelpText = "The fully qualified name of the visitor type to base the generation on.")]
    public string Visitor { get; set; } = string.Empty;
}

internal class Program
{
    internal static async Task Main(string[] args)
    {
        MSBuildLocator.RegisterDefaults();
        var workspace = MSBuildWorkspace.Create();

        await Parser.Default.ParseArguments<
            GreenTreeOptions,
            RedTreeOptions,
            VisitorInterfaceOptions,
            VisitorBaseOptions>(args)
        .MapResult(
            async (GreenTreeOptions opts) =>
            {
                var project = await workspace.OpenProjectAsync(opts.Project);
                var compilation = await project.GetCompilationAsync();
                var rootType = compilation?.GetTypeByMetadataName(opts.TreeRoot);
                if (rootType is null)
                {
                    Console.Error.WriteLine($"Could not load {opts.TreeRoot} from {opts.Project}");
                    return 1;
                }
                var code = GreenTreeGenerator.Generate(new(rootType));
                Console.WriteLine(code);
                return 0;
            },
            async (RedTreeOptions opts) =>
            {
                var project = await workspace.OpenProjectAsync(opts.Project);
                var compilation = await project.GetCompilationAsync();
                var rootType = compilation?.GetTypeByMetadataName(opts.TreeRoot);
                if (rootType is null)
                {
                    Console.Error.WriteLine($"Could not load {opts.TreeRoot} from {opts.Project}");
                    return 1;
                }
                var code = RedTreeGenerator.Generate(rootType);
                Console.WriteLine(code);
                return 0;
            },
            async (VisitorInterfaceOptions opts) =>
            {
                var project = await workspace.OpenProjectAsync(opts.Project);
                var compilation = await project.GetCompilationAsync();
                var rootType = compilation?.GetTypeByMetadataName(opts.TreeRoot);
                if (rootType is null)
                {
                    Console.Error.WriteLine($"Could not load {opts.TreeRoot} from {opts.Project}");
                    return 1;
                }
                var visitorType = compilation?.GetTypeByMetadataName(opts.Visitor);
                if (visitorType is null)
                {
                    Console.Error.WriteLine($"Could not load {opts.Visitor} from {opts.Project}");
                    return 1;
                }
                var code = VisitorGenerator.GenerateInterface(rootType, visitorType);
                Console.WriteLine(code);
                return 0;
            },
            async (VisitorBaseOptions opts) =>
            {
                var project = await workspace.OpenProjectAsync(opts.Project);
                var compilation = await project.GetCompilationAsync();
                var rootType = compilation?.GetTypeByMetadataName(opts.TreeRoot);
                if (rootType is null)
                {
                    Console.Error.WriteLine($"Could not load {opts.TreeRoot} from {opts.Project}");
                    return 1;
                }
                var visitorType = compilation?.GetTypeByMetadataName(opts.Visitor);
                if (visitorType is null)
                {
                    Console.Error.WriteLine($"Could not load {opts.Visitor} from {opts.Project}");
                    return 1;
                }
                var code = VisitorGenerator.GenerateBase(rootType, visitorType);
                Console.WriteLine(code);
                return 0;
            },
            errs =>
            {
                foreach (var err in errs) Console.Error.WriteLine(err);
                return Task.FromResult(1);
            });
    }
}
