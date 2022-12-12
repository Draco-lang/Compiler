using System.Linq;
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
        public bool GenerateWidthProperty { get; set; } = true;
        public string GetWidthMethodName { get; set; } = "GetWidth";
        public string WidthPropertyName { get; set; } = "Width";
        public bool GenerateChildrenProperty { get; set; } = true;
        public string GetChildrenMethodName { get; set; } = "GetChildren";
        public string ChildrenPropertyName { get; set; } = "Children";

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
    private bool DoGenerateWidthProperty => this.settings.GenerateWidthProperty;
    private bool DoGenerateChildrenProperty => this.settings.GenerateChildrenProperty;
    private string GetWidthMethodName => this.settings.GetWidthMethodName;
    private string GetChildrenMethodName => this.settings.GetChildrenMethodName;
    private string WidthPropertyName => this.settings.WidthPropertyName;
    private string ChildrenPropertyName => this.settings.ChildrenPropertyName;
    private string WidthFieldName => ToCamelCase(this.WidthPropertyName);

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
            .Write("#nullable enable")
            .Write(this.contentWriter)
            .Write("#nullable restore")
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
            if (SymbolEquals(nest, type)) this.contentWriter.Write(this.GeneratedAttribute);
            this.contentWriter
                .Write(nest.DeclaredAccessibility)
                .Write(nest.GetTypeKind(partial: true))
                .Write(nest.Name)
                .Write("{");
        }

        if (this.DoGenerateWidthProperty) this.GenerateWidthProperty(type);
        if (this.DoGenerateChildrenProperty) this.GenerateChildrenProperty(type);

        // Close braces
        foreach (var _ in type.EnumerateNestingChain()) this.contentWriter.Write("}");
    }

    private void GenerateWidthProperty(INamedTypeSymbol type)
    {
        if (type.IsAbstract) return;
        if (type.GetSanitizedProperties().Where(p => p.Name == this.WidthPropertyName).FirstOrDefault()?.IsOverride == true) return;

        // Sum up each member that has a Width attribute
        // For simplicity, we call a GetWidth that the user can roll themselves
        var memberWidths = type
            .GetSanitizedProperties()
            .Select(m => $"{this.GetWidthMethodName}(this.{m.Name})");
        this.contentWriter
            .Write($"private int? {this.WidthFieldName};")
            .Write(this.GeneratedAttribute)
            .Write($"""
            public override int {this.WidthPropertyName} =>
                this.{this.WidthFieldName} ??= {string.Join("+", memberWidths)};
            """);
    }

    private void GenerateChildrenProperty(INamedTypeSymbol type)
    {
        if (type.IsAbstract) return;
        if (type.GetSanitizedProperties().Where(p => p.Name == this.ChildrenPropertyName).FirstOrDefault()?.IsOverride == true) return;

        // For simplicity, we call a GetChildren that the user can roll themselves
        var memberChildren = type
            .GetSanitizedProperties()
            .Where(p => !p.IsGenerated())
            .Select(m => $"{this.GetChildrenMethodName}(this.{m.Name})");
        this.contentWriter
            .Write(this.GeneratedAttribute)
            .Write($$"""
            public override System.Collections.Generic.IEnumerable<{{this.RootType.ToDisplayString()}}> {{this.ChildrenPropertyName}}
            {
                get
                {
            """);
        foreach (var memberChild in memberChildren)
        {
            this.contentWriter.Write($"foreach (var c in {memberChild}) yield return c;");
        }
        this.contentWriter.Write("""
                }
            }
            """);
    }
}
