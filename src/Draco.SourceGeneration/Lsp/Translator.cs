using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Cs = Draco.SourceGeneration.Lsp.CsModel;
using Ts = Draco.SourceGeneration.Lsp.Metamodel;

namespace Draco.SourceGeneration.Lsp;

/// <summary>
/// Translates the TS-based metamodel into a C# model.
/// </summary>
internal sealed class Translator
{
    private readonly Ts.MetaModel sourceModel;
    private readonly Cs.Model targetModel = new();

    private readonly Dictionary<string, Cs.Type> translatedTypes = new();

    private readonly Dictionary<Ts.Structure, Cs.Class> structureClasses = new();
    private readonly Dictionary<Ts.Structure, Cs.Interface> structureInterfaces = new();

    public Translator(Ts.MetaModel sourceModel)
    {
        this.sourceModel = sourceModel;
    }

    /// <summary>
    /// Adds a builtin type that does not need translation anymore.
    /// </summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="type">The reflected type.</param>
    public void AddBuiltinType(string name, System.Type type) =>
        this.AddBuiltinType(name, type.FullName);

    /// <summary>
    /// Adds a builtin type that does not need translation anymore.
    /// </summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="fullName">The full name of the type.</param>
    public void AddBuiltinType(string name, string fullName) =>
        this.translatedTypes.Add(name, new Cs.BuiltinType(fullName));

    public Cs.Model Translate()
    {
        // Translate
        foreach (var structure in this.sourceModel.Structures)
        {
            var @class = this.TranslateStructure(structure);
            this.targetModel.Declarations.Add(@class);
        }
        foreach (var enumeration in this.sourceModel.Enumerations)
        {
            var @enum = this.TranslateEnumeration(enumeration);
            this.targetModel.Declarations.Add(@enum);
        }
        foreach (var typeAlias in this.sourceModel.TypeAliases) this.TranslateTypeAlias(typeAlias);

        // Add translated interfaces
        foreach (var @interface in this.structureInterfaces.Values)
        {
            this.targetModel.Declarations.Add(@interface);
        }

        // Connect up hierarchy
        foreach (var @class in this.targetModel.Declarations.OfType<Cs.Class>()) @class.InitializeParents();

        return this.targetModel;
    }

    private Cs.Class TranslateStructure(Ts.Structure structure)
    {
        if (this.structureClasses.TryGetValue(structure, out var existing)) return existing;

        var result = TranslateDeclaration<Cs.Class>(structure);
        this.structureClasses.Add(structure, result);
        this.translatedTypes.Add(structure.Name, new Cs.DeclarationType(result));

        foreach (var @base in structure.Extends)
        {
            var @interface = this.TranslateBaseType(@base);
            result.Interfaces.Add(@interface);
        }

        // TODO: Nested types

        // The properties will be an aggregation of
        //  - implemented interface properties (transitive closure)
        //  - mixin properties (transitive closure)
        //  - properties from the structure

        var allInterfaceProps = structure.Extends
            .Select(this.FindStructureByType)
            .SelectMany(s => TransitiveClosure(s, s => s.Extends.Select(this.FindStructureByType)))
            .SelectMany(i => i.Properties);
        var allMixinProps = structure.Mixins
            .Select(this.FindStructureByType)
            .SelectMany(s => TransitiveClosure(s, s => s.Mixins.Select(this.FindStructureByType)))
            .SelectMany(s => s.Properties);

        var allProps = allInterfaceProps
            .Concat(allMixinProps)
            .Concat(structure.Properties);

        // We deduplicate the properties, only keeping the last occurrence, so
        // mixin takes priority over interface ans structure takes priority over mixin
        allProps = allProps
            .GroupBy(p => p.Name)
            .Select(g => g.Last());

        foreach (var prop in allProps)
        {
            var csProp = this.TranslateProperty(result, prop);
            result.Properties.Add(csProp);
        }

        return result;
    }

    private Cs.Interface TranslateStructureAsInterface(Ts.Structure structure)
    {
        if (this.structureInterfaces.TryGetValue(structure, out var existing)) return existing;

        var result = TranslateDeclaration<Cs.Interface>(structure);
        this.structureInterfaces.Add(structure, result);

        foreach (var @base in structure.Extends)
        {
            var @interface = this.TranslateBaseType(@base);
            result.Interfaces.Add(@interface);
        }

        // TODO: Nested types

        foreach (var prop in structure.Properties)
        {
            var csProp = this.TranslateProperty(result, prop);
            result.Properties.Add(csProp);
        }

        // Prefix the interface name to follow C# conventions
        // NOTE: We do it here on purpose, to not to affect property name resolution
        result.Name = $"I{result.Name}";

        return result;
    }

    private Cs.Enum TranslateEnumeration(Ts.Enumeration enumeration)
    {
        var result = TranslateDeclaration<Cs.Enum>(enumeration);

        // TODO

        return result;
    }

    private void TranslateTypeAlias(Ts.TypeAlias typeAlias)
    {
        // TODO
    }

    private Cs.Interface TranslateBaseType(Ts.Type type)
    {
        var structure = this.FindStructureByType(type);
        return this.TranslateStructureAsInterface(structure);
    }

    private Ts.Structure FindStructureByType(Ts.Type type)
    {
        if (type is not Ts.NamedType namedType)
        {
            throw new ArgumentException($"can not reference type of kind {type.GetType().Name}", nameof(type));
        }

        var structure = this.sourceModel.Structures.FirstOrDefault(s => s.Name == namedType.Name)
                     ?? throw new ArgumentException($"can not find type {namedType.Name} among the structures", nameof(type));

        return structure;
    }

    private Cs.Property TranslateProperty(Cs.Declaration parent, Ts.Property property)
    {
        var result = TranslateDeclaration<Cs.Property>(property);

        // There can be name collisions, check for that
        if (result.Name == parent.Name) result.Name = $"{result.Name}_";

        result.SerializedName = property.Name;
        // TODO: Temporary
        result.Type = new Cs.BuiltinType("System.Int32");

        return result;
    }

    private static TDeclaration TranslateDeclaration<TDeclaration>(Ts.IDocumented source)
        where TDeclaration : Cs.Declaration, new()
    {
        var target = new TDeclaration();

        if (source is Ts.IDeclaration declSource) target.Name = Capitalize(declSource.Name);

        target.Documentation = source.Documentation;
        target.Deprecated = source.Deprecated;
        target.SinceVersion = source.Since;
        target.IsProposed = source.Proposed ?? false;

        return target;
    }

    private static IEnumerable<T> TransitiveClosure<T>(T element, Func<T, IEnumerable<T>> neighbors)
    {
        yield return element;
        foreach (var n in neighbors(element)) yield return n;
    }

    /// <summary>
    /// Capitalizes a word.
    /// </summary>
    /// <param name="word">The word to capitalize.</param>
    /// <returns>The capitalized <paramref name="word"/>.</returns>
    [return: NotNullIfNotNull(nameof(word))]
    private static string? Capitalize(string? word) => word is null
        ? null
        : $"{char.ToUpper(word[0])}{word.Substring(1)}";
}
