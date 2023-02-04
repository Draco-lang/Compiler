using Draco.SourceGeneration.SyntaxTree;
using Scriban.Runtime;
using Scriban;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Threading;

namespace Draco.SourceGeneration;

internal static class CodeGenerator
{
    public static string GenerateGreenTree(Tree tree, CancellationToken cancellationToken) =>
        Render("GreenTree.sbncs", tree, cancellationToken);
    public static string GenerateRedTree(Tree tree, CancellationToken cancellationToken) =>
        Render("RedTree.sbncs", tree, cancellationToken);

    private static string Render(string templateName, object model, CancellationToken cancellationToken)
    {
        var template = ScribanTemplateLoader.Load(templateName);

        var context = new TemplateContext
        {
            TemplateLoader = ScribanTemplateLoader.Instance,
            MemberRenamer = MemberRenamer,
        };
        var scriptObject = new ScriptObject();
        scriptObject.Import(ScribanHelperFunctions.Instance);
        scriptObject.Import(model, renamer: MemberRenamer);
        context.PushGlobal(scriptObject);
        context.CancellationToken = cancellationToken;

        var output = template.Render(context);
        return SyntaxFactory
            .ParseCompilationUnit(output)
            .NormalizeWhitespace()
            .GetText()
            .ToString();
    }

    private static string MemberRenamer(MemberInfo memberInfo) => memberInfo.Name;
}
