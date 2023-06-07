using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Scriban;
using Scriban.Runtime;

namespace Draco.SourceGeneration;

internal static class CodeGenerator
{
    public static string GenerateGreenSyntaxTree(SyntaxTree.Tree tree, CancellationToken cancellationToken) =>
        Render("GreenSyntaxTree.sbncs", tree, cancellationToken);
    public static string GenerateRedSyntaxTree(SyntaxTree.Tree tree, CancellationToken cancellationToken) =>
        Render("RedSyntaxTree.sbncs", tree, cancellationToken);
    public static string GenerateUntypedTree(UntypedTree.Tree tree, CancellationToken cancellationToken) =>
        Render("UntypedTree.sbncs", tree, cancellationToken);
    public static string GenerateBoundTree(BoundTree.Tree tree, CancellationToken cancellationToken) =>
        Render("BoundTree.sbncs", tree, cancellationToken);
    public static string GenerateOneOf(OneOf.Config config, CancellationToken cancellationToken) =>
        Render("OneOf.sbncs", config, cancellationToken);
    public static string GenerateLspModel(Lsp.CsModel.Model model, CancellationToken cancellationToken) =>
        Render("LspModel.sbncs", model, cancellationToken);
    public static string GenerateDapModel(Dap.CsModel.Model model, CancellationToken cancellationToken) =>
        Render("DapModel.sbncs", model, cancellationToken);
    public static string GenerateWellKnownTypes(WellKnownTypes.WellKnownTypes model, CancellationToken cancellationToken) =>
        Render("WellKnownTypes.sbncs", model, cancellationToken);

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
