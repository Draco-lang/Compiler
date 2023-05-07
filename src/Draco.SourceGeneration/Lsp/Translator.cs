using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
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

    /// <summary>
    /// Translates the given type by name.
    /// </summary>
    /// <param name="name">The name of the type to translate.</param>
    /// <returns>The translated C# type reference.</returns>
    public Cs.Type TranslateTypeByName(string name)
    {
        if (this.translatedTypes.TryGetValue(name, out var translated)) return translated;

        var structure = this.sourceModel.Structures.FirstOrDefault(s => s.Name == name);
        if (structure is not null)
        {
            // If the structure is used as a base type for other types, we return the interface to be referenced instead
            var usedAsInterface = this.sourceModel.Structures
                .Any(s => s.Extends.OfType<Ts.NamedType>().Any(t => t.Name == name));

            return usedAsInterface
                ? new Cs.DeclarationType(this.TranslateStructureAsInterface(structure))
                : this.TranslateStructure(structure);
        }

        var enumeration = this.sourceModel.Enumerations.FirstOrDefault(s => s.Name == name);
        if (enumeration is not null)
        {
            return this.TranslateEnumeration(enumeration);
        }

        var alias = this.sourceModel.TypeAliases.FirstOrDefault(s => s.Name == name);
        if (alias is not null)
        {
            this.TranslateTypeAlias(alias);
            return this.translatedTypes[name];
        }

        // Marker for unresolved types
        return new Cs.BuiltinType($"NOT_FOUND<{name}>");
    }

    /// <summary>
    /// Translated the source model to a C# model.
    /// </summary>
    /// <returns>The translated C# model.</returns>
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

        // The properties will be an aggregation of
        //  - implemented interface properties (transitive closure)
        //  - mixin properties (transitive closure)
        //  - properties from the structure

        var allInterfaceProps = structure.Extends
            .Select(this.FindStructureByType)
            .SelectMany(s => TransitiveClosure(s, s => s.Extends.Select(this.FindStructureByType)))
            .SelectMany(i => i.Properties.Select(prop => (Parent: i, Property: prop)));
        var allMixinProps = structure.Mixins
            .Select(this.FindStructureByType)
            .SelectMany(s => TransitiveClosure(s, s => s.Mixins.Select(this.FindStructureByType)))
            .SelectMany(s => s.Properties.Select(prop => (Parent: s, Property: prop)));

        var allProps = allInterfaceProps
            .Concat(allMixinProps)
            .Concat(structure.Properties.Select(prop => (Parent: structure, Property: prop)));

        // We deduplicate the properties, only keeping the last occurrence, so
        // mixin takes priority over interface ans structure takes priority over mixin
        allProps = allProps
            .GroupBy(p => p.Property.Name)
            .Select(g => g.Last());

        foreach (var (parent, prop) in allProps)
        {
            var csParent = (Cs.Class)((Cs.DeclarationType)this.TranslateStructure(parent)).Declaration;
            var csProp = this.TranslateProperty(csParent, prop);
            result.Properties.Add(csProp);
        }

        return resultRef;
    }

    private Cs.Interface TranslateStructureAsInterface(Ts.Structure structure)
    {
        if (this.structureInterfaces.TryGetValue(structure, out var existing)) return existing;

        var csClass = (Cs.Class)((Cs.DeclarationType)this.TranslateStructure(structure)).Declaration;
        // Prefix the interface name to follow C# conventions
        var result = TranslateDeclaration<Cs.Interface>(structure);
        result.Name = $"I{result.Name}";
        this.structureInterfaces.Add(structure, result);
        this.targetModel.Declarations.Add(result);

        foreach (var @base in structure.Extends)
        {
            var @interface = this.TranslateBaseType(@base);
            result.Interfaces.Add(@interface);
        }

        foreach (var prop in structure.Properties)
        {
            // NOTE: We pass in the class on purose
            var csProp = this.TranslateProperty(csClass, prop);
            result.Properties.Add(csProp);
        }

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

        var aliasedType = this.TranslateType(typeAlias.Type, parent: null, hintName: typeAlias.Name);

        // We can cause a duplicate key in case this is an alias for an anonymous type
        if (!this.translatedTypes.ContainsKey(typeAlias.Name))
        {
            this.translatedTypes.Add(typeAlias.Name, aliasedType);
        }
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

    private Cs.Property TranslateProperty(Cs.Class parent, Ts.Property property)
    {
        var result = TranslateDeclaration<Cs.Property>(property);

        // There can be name collisions, check for that
        if (result.Name == parent.Name) result.Name = $"{result.Name}_";

        result.SerializedName = property.Name;
        result.Type = this.TranslateType(property.Type, parent: parent, hintName: result.Name);

        if (property.IsOptional)
        {
            result.OmitIfNull = true;
            // If the type is not nullable, we make it one
            if (result.Type is not Cs.NullableType) result.Type = new Cs.NullableType(result.Type);
        }

        if (property.Type.Kind == "stringLiteral")
        {
            // It has a constant value
            var literalType = (Ts.LiteralType)property.Type;
            result.Value = TranslateValue(literalType.Value);
        }

        return result;
    }

    private Cs.Type TranslateType(Ts.Type type, Cs.Class? parent, string? hintName)
    {
        switch (type.Kind)
        {
        case "stringLiteral":
            return this.TranslateTypeByName("string");
        case "base":
        case "reference":
        {
            var namedType = (Ts.NamedType)type;
            return this.TranslateTypeByName(namedType.Name);
        }
        case "array":
        {
            var arrayType = (Ts.ArrayType)type;
            var elementType = this.TranslateType(arrayType.Element, parent: parent, hintName: hintName);
            return new Cs.ArrayType(elementType);
        }
        case "map":
        {
            var mapType = (Ts.MapType)type;
            var keyType = this.TranslateType(mapType.Key, parent: parent, hintName: hintName);
            var valueType = this.TranslateType(mapType.Value, parent: parent, hintName: hintName);
            return new Cs.DictionaryType(keyType, valueType);
        }
        case "or":
        {
            Ts.Type UnwrapAlias(Ts.Type t)
            {
                if (t.Kind != "base" && t.Kind != "reference") return t;
                var namedType = (Ts.NamedType)t;
                return this.sourceModel.TypeAliases.FirstOrDefault(t => t.Name == namedType.Name)?.Type ?? t;
            }
            static bool IsNull(Ts.Type t) => t is Ts.NamedType { Kind: "base", Name: "null" };
            static bool IsOr(Ts.Type t) => t is Ts.AggregateType { Kind: "or" };
            static bool IsStructureLiteral(Ts.Type t) => t is Ts.StructureLiteralType;

            var aggregateType = (Ts.AggregateType)type;
            var items = aggregateType.Items
                .Select(UnwrapAlias)
                .ToImmutableArray();

            // If it's a singular type, just translate that
            if (items.Length == 1) return this.TranslateType(items[0], parent: parent, hintName: hintName);

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
                    Items = items.AsEquatableArray(),
                    Kind = "or",
                };
                return this.TranslateType(tsSubtype, parent: parent, hintName: hintName);
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
                    Items = items.AsEquatableArray(),
                    Kind = "or",
                };
                var subtype = this.TranslateType(tsSubtype, parent: parent, hintName: hintName);
                return new Cs.NullableType(subtype);
            }

            // If we have multiple literal types, merge them
            if (items.Count(IsStructureLiteral) > 1)
            {
                var literals = items
                    .OfType<Ts.StructureLiteralType>()
                    .Select(l => l.Value);
                var mergedLiteral = MergeStructureLiterals(literals);

                items = items
                    .Where(i => !IsStructureLiteral(i))
                    .Append(mergedLiteral)
                    .ToImmutableArray();
                var tsSubtype = new Ts.AggregateType
                {
                    Items = items.AsEquatableArray(),
                    Kind = "or",
                };
                return this.TranslateType(tsSubtype, parent: parent, hintName: hintName);
            }

            // Just a general DU
            var alternatives = items
                .Select(i => this.TranslateType(i, parent: parent, hintName: hintName))
                .ToImmutableArray();
            return new Cs.DiscriminatedUnionType(alternatives);
        }
        case "tuple":
        {
            var aggregateType = (Ts.AggregateType)type;
            var items = aggregateType.Items
                .Select(i => this.TranslateType(i, parent: parent, hintName: hintName))
                .ToImmutableArray();
            return new Cs.TupleType(items);
        }
        case "literal":
        {
            var structLiteral = (Ts.StructureLiteralType)type;
            if (hintName is null) throw new InvalidOperationException("no hint name for anonymous type");

            // Translate the literal
            var result = TranslateDeclaration<Cs.Class>(structLiteral.Value);
            result.Parent = parent;
            result.Name = parent is null
                ? hintName
                : $"{hintName}{ExtractNameSuffix(parent.Name)}";

            // Add to parent type or global decls
            if (parent is null)
            {
                // Check if already exists
                if (this.translatedTypes.TryGetValue(result.Name, out var existing)) return existing;

                this.targetModel.Declarations.Add(result);
                this.translatedTypes.Add(result.Name, new Cs.DeclarationType(result));
            }
            else
            {
                // Check if already exists
                var nestedType = parent.NestedDeclarations.FirstOrDefault(d => d.Name == result.Name);
                if (nestedType is not null) return new Cs.DeclarationType(nestedType);

                parent.NestedDeclarations.Add(result);
            }

            foreach (var prop in structLiteral.Value.Properties)
            {
                var csProp = this.TranslateProperty(result, prop);
                result.Properties.Add(csProp);
            }

            return new Cs.DeclarationType(result);
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
    /// Merges multiple structure literal types into one.
    /// </summary>
    /// <param name="literals">The literal types to merge.</param>
    /// <returns>A single type, containing all fields of <paramref name="literals"/>.</returns>
    private static Ts.Type MergeStructureLiterals(IEnumerable<Ts.StructureLiteral> literals)
    {
        // Don't compare documentation, optionality, etc.
        var comparer = MappingEqualityComparer.Create((Ts.Property p) => (p.Name, p.Type));

        var intersection = new HashSet<Ts.Property>(literals.First().Properties, comparer);
        var union = new HashSet<Ts.Property>(comparer);

        foreach (var alt in literals)
        {
            intersection.IntersectWith(alt.Properties);
            union.UnionWith(alt.Properties);
        }

        union.ExceptWith(intersection);

        // Prefer the nullable version of the property if it exists
        var always = intersection.GroupBy(i => i.Name).Select(g => g.OrderBy(p => p.IsOptional).First());
        var optional = union.Select(a => a with { Optional = true }).Distinct(comparer);

        return new Ts.StructureLiteralType
        {
            Kind = "literal",
            Value = new Ts.StructureLiteral
            {
                Properties = always.Concat(optional).ToEquatableArray()
            }
        };
    }

    /// <summary>
    /// Extracts a suffix from a name, which is the last capitalized word in it.
    /// </summary>
    /// <param name="name">The name to extract the suffix from.</param>
    /// <returns>The last capitalized word in <paramref name="name"/>.</returns>
    private static string ExtractNameSuffix(string name)
    {
        // Search for the last uppercase letter
        var startIndex = name.Length - 1;
        for (; startIndex >= 0; --startIndex)
        {
            var ch = name[startIndex];
            if (ch == char.ToUpper(ch)) break;
        }
        // Cut it off
        return name[startIndex..];
    }

    /// <summary>
    /// Capitalizes a word.
    /// </summary>
    /// <param name="word">The word to capitalize.</param>
    /// <returns>The capitalized <paramref name="word"/>.</returns>
    [return: NotNullIfNotNull(nameof(word))]
    private static string? Capitalize(string? word) => word is null
        ? null
        : $"{char.ToUpper(word[0])}{word[1..]}";
}
