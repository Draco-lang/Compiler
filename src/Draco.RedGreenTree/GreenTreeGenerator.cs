using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

/// <summary>
/// Implements boilerplate code for green nodes.
/// </summary>
public sealed class GreenTreeGenerator : GeneratorBase
{
    public sealed class Settings
    {
        public INamedTypeSymbol RootType { get; set; }
        public bool GenerateWidth { get; set; } = true;
        public bool GenerateCtor { get; set; } = true;
        public string GetWidthMethodName { get; set; } = "GetWidth";
        public string WidthPropertyName { get; set; } = "Width";
        public string WidthFieldName { get; set; } = "width";

        public Settings(INamedTypeSymbol rootType)
        {
            this.RootType = rootType;
        }
    }

    public static string Generate(Settings settings)
    {
        var generator = new GreenTreeGenerator(settings);
        return generator.Generate();
    }

    private readonly Settings settings;
    private readonly CodeWriter headerWriter = new();
    private readonly CodeWriter contentWriter = new();

    private INamedTypeSymbol RootType => this.settings.RootType;
    private bool GenerateWidth => this.settings.GenerateWidth;
    private bool GenerateCtor => this.settings.GenerateCtor;
    private string GetWidthMethodName => this.settings.GetWidthMethodName;
    private string WidthPropertyName => this.settings.WidthPropertyName;
    private string WidthFieldName => this.settings.WidthFieldName;

    private GreenTreeGenerator(Settings settings)
    {
        this.settings = settings;
    }

    protected override string Generate()
    {
        this.GenerateHeader();
        this.GenerateNamespace();
        this.GenerateTree();

        return new CodeWriter()
            .Write(this.headerWriter)
            .Write(this.contentWriter)
            .ToString();
    }

    private void GenerateHeader()
    {
        this.headerWriter
            .Write(this.HeaderComment)
            .Write("//")
            .Write(SettingsToHeaderComment(this.settings));
    }

    private void GenerateNamespace()
    {
        if (this.RootType.ContainingNamespace is null) return;
        this.contentWriter
            .Write("namespace ")
            .Write(this.RootType.ContainingNamespace)
            .Write(";");
    }

    private void GenerateTree()
    {
        foreach (var type in this.RootType.EnumerateContainedTypeTree())
        {
            if (!type.IsSubtypeOf(this.RootType)) continue;
            this.GenerateGreenNode(type);
        }
    }

    private void GenerateGreenNode(INamedTypeSymbol type)
    {
        // Wrapping types
        foreach (var nest in type.EnumerateNestingChain())
        {
            this.contentWriter
                .Write(nest.DeclaredAccessibility)
                .Write(nest.GetTypeKind(partial: true))
                .Write(nest.Name)
                .Write("{");
        }

        if (this.GenerateWidth) this.GenerateWidthProperty(type);
        if (this.GenerateCtor) this.GenerateCtorMethod(type);

        // Close braces
        foreach (var _ in type.EnumerateNestingChain()) this.contentWriter.Write("}");
    }

    private void GenerateWidthProperty(INamedTypeSymbol type)
    {
        if (type.IsAbstract) return;

        // Sum up each member that has a Width attribute
        // For simplicity, we call a GetWidth that the user can roll themselves
        var memberWidths = type
            .GetSanitizedProperties()
            .Select(m => $"{this.GetWidthMethodName}(this.{m.Name})");
        this.contentWriter
            .Write($"private int? {this.WidthFieldName};")
            .Write($"""
            public override int {this.WidthPropertyName} =>
                this.{this.WidthFieldName} ??= {string.Join("+", memberWidths)};
            """);
    }

    private void GenerateCtorMethod(INamedTypeSymbol type)
    {
        if (type.IsAbstract) return;

        // TODO
    }
}
