using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
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

    // TODO: If we are referencing a structure that could have an interface, return that instead
    public Cs.Type TranslateTypeByName(string name)
    {
        if (this.translatedTypes.TryGetValue(name, out var translated)) return translated;

        var structure = this.sourceModel.Structures.FirstOrDefault(s => s.Name == name);
        if (structure is not null)
        {
            this.TranslateStructure(structure);
            return this.translatedTypes[name];
        }

        var enumeration = this.sourceModel.Enumerations.FirstOrDefault(s => s.Name == name);
        if (enumeration is not null)
        {
            this.TranslateEnumeration(enumeration);
            return this.translatedTypes[name];
        }

        var alias = this.sourceModel.TypeAliases.FirstOrDefault(s => s.Name == name);
        if (alias is not null)
        {
            this.TranslateTypeAlias(alias);
            return this.translatedTypes[name];
        }

        // TODO
        return new Cs.BuiltinType($"NOT_FOUND<{name}>");
    }

    public Cs.Model Translate()
    {
        // Translate
        foreach (var structure in this.sourceModel.Structures)
        {
            // Check if a builtin has overriden it already
            if (this.translatedTypes.ContainsKey(structure.Name)) continue;
            this.TranslateStructure(structure);
        }
        foreach (var enumeration in this.sourceModel.Enumerations)
        {
            // Check if a builtin has overriden it already
            if (this.translatedTypes.ContainsKey(enumeration.Name)) continue;
            this.TranslateEnumeration(enumeration);
        }
        foreach (var typeAlias in this.sourceModel.TypeAliases)
        {
            // Check if a builtin has overriden it already
            if (this.translatedTypes.ContainsKey(typeAlias.Name)) continue;
            this.TranslateTypeAlias(typeAlias);
        }

        // Connect up hierarchy
        foreach (var @class in this.targetModel.Declarations.OfType<Cs.Class>()) @class.InitializeParents();

        return this.targetModel;
    }

    private Cs.Type TranslateStructure(Ts.Structure structure)
    {
        if (this.translatedTypes.TryGetValue(structure.Name, out var existing)) return existing;

        var result = TranslateDeclaration<Cs.Class>(structure);
        var resultRef = new Cs.DeclarationType(result);
        this.translatedTypes.Add(structure.Name, resultRef);
        this.targetModel.Declarations.Add(result);

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

        return resultRef;
    }

    private Cs.Interface TranslateStructureAsInterface(Ts.Structure structure)
    {
        if (this.structureInterfaces.TryGetValue(structure, out var existing)) return existing;

        var result = TranslateDeclaration<Cs.Interface>(structure);
        this.structureInterfaces.Add(structure, result);
        this.targetModel.Declarations.Add(result);

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

    private Cs.Type TranslateEnumeration(Ts.Enumeration enumeration)
    {
        if (this.translatedTypes.TryGetValue(enumeration.Name, out var existing)) return existing;

        var result = TranslateDeclaration<Cs.Enum>(enumeration);
        var resultRef = new Cs.DeclarationType(result);
        this.translatedTypes.Add(enumeration.Name, resultRef);
        this.targetModel.Declarations.Add(result);

        foreach (var member in enumeration.Values)
        {
            this.TranslateEnumerationEntry(result, member);
        }

        return resultRef;
    }

    private void TranslateEnumerationEntry(Cs.Enum @enum, Ts.EnumerationEntry member)
    {
        var result = TranslateDeclaration<Cs.EnumMember>(member);
        @enum.Members.Add(result);
        result.Value = TranslateValue(member.Value);
    }

    private void TranslateTypeAlias(Ts.TypeAlias typeAlias)
    {
        if (this.translatedTypes.ContainsKey(typeAlias.Name)) return;

        var aliasedType = this.TranslateType(typeAlias.Type);
        this.translatedTypes.Add(typeAlias.Name, aliasedType);
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
        result.Type = this.TranslateType(property.Type);

        return result;
    }

    private Cs.Type TranslateType(Ts.Type type)
    {
        switch (type.Kind)
        {
        case "base":
        case "reference":
        {
            var namedType = (Ts.NamedType)type;
            return this.TranslateTypeByName(namedType.Name);
        }
        case "array":
        {
            var arrayType = (Ts.ArrayType)type;
            var elementType = this.TranslateType(arrayType.Element);
            return new Cs.ArrayType(elementType);
        }
        case "map":
        {
            var mapType = (Ts.MapType)type;
            var keyType = this.TranslateType(mapType.Key);
            var valueType = this.TranslateType(mapType.Value);
            return new Cs.DictionaryType(keyType, valueType);
        }
        case "or":
        {
            Ts.Type UnwrapAlias(Ts.Type t)
            {
                if (t.Kind != "base" && t.Kind != "reference") return t;
                var namedType = (Ts.NamedType)t;
                return this.sourceModel.TypeAliases.FirstOrDefault(t => t.Name == namedType.Name)?.Type
                    ?? t;
            }
            static bool IsNull(Ts.Type t) => t is Ts.NamedType { Kind: "base", Name: "null" };
            static bool IsOr(Ts.Type t) => t is Ts.AggregateType { Kind: "or" };

            var aggregateType = (Ts.AggregateType)type;
            var items = aggregateType.Items
                .Select(UnwrapAlias)
                .ToImmutableArray();

            // If it's a singular type, just translate that
            if (items.Length == 1) return this.TranslateType(items[0]);

            // We have nested OR types, flatten
            if (items.Any(IsOr))
            {
                // Keep non-or types and merge in OR'd types
                items = items
                    .Where(i => !IsOr(i))
                    .Concat(items
                        .Where(IsOr)
                        .Cast<Ts.AggregateType>()
                        .SelectMany(i => i.Items))
                    .ToImmutableArray();
                var tsSubtype = new Ts.AggregateType
                {
                    Items = items,
                    Kind = "or",
                };
                return this.TranslateType(tsSubtype);
            }

            // We have a null OR'd into the type, for example number | string | null
            if (items.Any(IsNull))
            {
                // We remove it and make the type nullable instead
                items = items
                    .Where(i => !IsNull(i))
                    .ToImmutableArray();
                var tsSubtype = new Ts.AggregateType
                {
                    Items = items,
                    Kind = "or",
                };
                var subtype = this.TranslateType(tsSubtype);
                return new Cs.NullableType(subtype);
            }

            // Just a general DU
            var alternatives = items
                .Select(this.TranslateType)
                .ToImmutableArray();
            return new Cs.DiscriminatedUnionType(alternatives);
        }
        case "tuple":
        {
            var aggregateType = (Ts.AggregateType)type;
            var items = aggregateType.Items
                .Select(this.TranslateType)
                .ToImmutableArray();
            return new Cs.TupleType(items);
        }
        default:
            return new Cs.BuiltinType($"UNKNOWN<{type.Kind}>");
        }
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

    private static object? TranslateValue(object? value) => value switch
    {
        JsonElement element => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetInt32(),
            _ => value,
        },
        _ => value,
    };

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
