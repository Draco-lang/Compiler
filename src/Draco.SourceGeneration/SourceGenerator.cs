using System;
using System.Collections.Generic;
using System.Text;
using Draco.SourceGeneration.SyntaxTree;
using Scriban.Runtime;
using Scriban;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Draco.SourceGeneration;

internal static class SourceGenerator
{
    public static string GenerateGreenTree(Tree tree) => Render("GreenTree.sbncs", tree);
    public static string GenerateRedTree(Tree tree) => Render("RedTree.sbncs", tree);

    private static string Render(string templateName, object model)
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

        var output = template.Render(context);
        return SyntaxFactory
            .ParseCompilationUnit(output)
            .NormalizeWhitespace()
            .GetText()
            .ToString();
    }

    private static string MemberRenamer(MemberInfo memberInfo) => memberInfo.Name;
}
