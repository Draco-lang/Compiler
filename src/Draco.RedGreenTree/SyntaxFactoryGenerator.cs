using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

/// <summary>
/// Generates factory functions for the tree.
/// </summary>
public sealed class SyntaxFactoryGenerator : GeneratorBase
{
    public sealed class Settings
    {
        public INamedTypeSymbol FactoryType { get; set; }
        public INamedTypeSymbol GreenRootType { get; set; }
        public INamedTypeSymbol RedRootType { get; set; }
        public Func<INamedTypeSymbol, string> GetRedName { get; set; } = x => x.Name;
        public string GreenPropertyName { get; set; } = "Green";
        public string ToRedMethodName { get; set; } = "ToRed";

        public Settings(
            INamedTypeSymbol factoryType,
            INamedTypeSymbol greenRootType,
            INamedTypeSymbol redRootType)
        {
            this.FactoryType = factoryType;
            this.GreenRootType = greenRootType;
            this.RedRootType = redRootType;
        }
    }

    public static string Generate(Settings settings)
    {
        var generator = new SyntaxFactoryGenerator(settings);
        return generator.Generate();
    }

    private readonly Settings settings;
    private readonly HashSet<INamedTypeSymbol> typesInParseTree = new(SymbolEqualityComparer.Default);
    private readonly CodeWriter headerWriter = new();
    private readonly CodeWriter contentWriter = new();
    private readonly HashSet<string> customMethodNames;

    private INamedTypeSymbol FactoryType => this.settings.FactoryType;
    private INamedTypeSymbol GreenRootType => this.settings.GreenRootType;
    private INamedTypeSymbol RedRootType => this.settings.RedRootType;
    private string GreenPropertyName => this.settings.GreenPropertyName;
    private string ToRedMethodName => this.settings.ToRedMethodName;
    private string GetRedClassName(INamedTypeSymbol type) => SymbolEquals(type, this.GreenRootType)
        ? this.RedRootType.Name
        : this.settings.GetRedName(type);
    private string GetFullRedClassName(INamedTypeSymbol type)
    {
        if (!this.typesInParseTree.Contains(type) && !type.IsSubtypeOf(this.settings.GreenRootType))
        {
            return type.ToDisplayString();
        }
        var typeName = string.Join(".", type.EnumerateNestingChain().Select(this.GetRedClassName));
        if (this.RedRootType.ContainingNamespace is null) return typeName;
        return $"{this.RedRootType.ContainingNamespace.ToDisplayString()}.{typeName}";
    }

    private SyntaxFactoryGenerator(Settings settings)
    {
        this.settings = settings;
        foreach (var item in settings.GreenRootType.EnumerateContainedTypeTree()) this.typesInParseTree.Add(item);

        this.customMethodNames = new();
        this.ExtractCustomFactoryMethods();
    }

    private void ExtractCustomFactoryMethods()
    {
        var result = new Dictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);
        var methods = this.FactoryType
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic);
        foreach (var m in methods) this.customMethodNames.Add(m.Name);
    }

    protected override string Generate()
    {
        this.GenerateHeader();
        this.GenerateNamespace();
        this.GenerateFactory();

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
        if (this.RedRootType.ContainingNamespace is null) return;
        this.contentWriter
            .Write("namespace ")
            .Write(this.RedRootType.ContainingNamespace)
            .Write(";");
    }

    private void GenerateFactory()
    {
        // Opening
        this.contentWriter
            .Write(this.FactoryType.DeclaredAccessibility)
            .Write(this.FactoryType.GetTypeKind(partial: true));

        foreach (var type in this.GreenRootType.EnumerateContainedTypeTree())
        {
            if (!type.IsSubtypeOf(this.GreenRootType)) continue;
            this.GenerateRedFactory(type);
        }

        // Closing brace
        this.contentWriter
            .Write("}");
    }

    private void GenerateRedFactory(INamedTypeSymbol greenType)
    {
        // TODO
    }
}
