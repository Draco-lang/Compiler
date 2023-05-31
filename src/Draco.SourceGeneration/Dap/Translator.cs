using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Draco.SourceGeneration.Dap.CsModel;
using Enum = Draco.SourceGeneration.Dap.CsModel.Enum;
using Type = Draco.SourceGeneration.Dap.CsModel.Type;

namespace Draco.SourceGeneration.Dap;

/// <summary>
/// Translates the JSON schema to a C# model.
/// </summary>
internal sealed class Translator
{
    private static readonly string[] basicStructures = new[]
    {
        "ProtocolMessage",
        "Request",
        "Response",
        "Event",
    };

    private readonly JsonDocument sourceModel;
    private readonly Model targetModel = new();
    private readonly Dictionary<string, Type> translatedTypes = new();

    public Translator(JsonDocument sourceModel)
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
        this.translatedTypes.Add(name, new CsModel.BuiltinType(fullName));

    /// <summary>
    /// Translated the source model to a C# model.
    /// </summary>
    /// <returns>The translated C# model.</returns>
    public Model Translate()
    {
        // Get all definitions in the schema
        var types = this.sourceModel.RootElement
            .GetProperty("definitions")
            .EnumerateObject();

        foreach (var type in types)
        {
            var typeName = type.Name;
            var typeDesc = type.Value;

            // We skip basic structures
            if (basicStructures.Contains(typeName)) continue;

            // We skip requests, as all of their content is already among definitions as arguments
            if (typeName.EndsWith("Request")) continue;

            var result = null as Type;

            // Responses and Events have a "body" property
            if (typeName.EndsWith("Response") || typeName.EndsWith("Event"))
            {
                var innerTypeDesc = typeDesc
                    .GetProperty("allOf")
                    .EnumerateArray()
                    .Last();

                if (innerTypeDesc.TryGetProperty("properties", out var props)
                 && props.TryGetProperty("body", out var body))
                {
                    result = this.TranslateType(body, nameHint: typeName, parent: null);

                    if (result is DeclarationType { Declaration: var innerDecl })
                    {
                        // Update docs
                        ExtractDocumentation(props, innerDecl);
                }
                }
                else
                {
                    var emptyClass = new Class() { Name = typeName };
                    this.ForwardDeclare(emptyClass);
                    result = new DeclarationType(emptyClass);
                }
            }

            // Regular type
            result ??= this.TranslateTypeByName(typeName);

            if (result is DeclarationType { Declaration: var decl })
            {
                // We update the docs with the original definition
                ExtractDocumentation(typeDesc, decl);
                // Add to the target model
                this.targetModel.Declarations.Add(decl);
            }
        }

        return this.targetModel;
    }

    private void ForwardDeclare(Declaration declaration) =>
        this.translatedTypes.Add(declaration.Name, new DeclarationType(declaration));

    private Type TranslateTypeByName(string name)
    {
        // Check, if already present
        if (this.translatedTypes.TryGetValue(name, out var existing)) return existing;

        // Get all definitions in the schema
        var types = this.sourceModel.RootElement
            .GetProperty("definitions")
            .EnumerateObject();

        foreach (var type in types)
        {
            if (type.Name == name) return this.TranslateType(type.Value, nameHint: name, parent: null);
        }

        throw new KeyNotFoundException($"the type {name} was not found by name");
    }

    private Type TranslateType(JsonElement description, string? nameHint, Class? parent)
    {
        static Class UnwrapClass(Type type) => (Class)((DeclarationType)type).Declaration;

        string GetDeclarationName()
        {
            if (nameHint is null) throw new ArgumentNullException(nameof(nameHint));
            return parent is null
                ? nameHint
                : $"{ExtractNamePrefix(parent.Name)}{nameHint}";
        }

        void Declare(Declaration declaration)
        {
            parent?.NestedDeclarations.Add(declaration);
            ExtractDocumentation(description, declaration);
            this.ForwardDeclare(declaration);
        }

        var typeTag = ExtractTypeTag(description);

        // Builtins
        if (typeTag is not null && this.translatedTypes.TryGetValue(typeTag, out var builtin)) return builtin;
        if (description.ValueKind == JsonValueKind.String) return this.translatedTypes[description.GetString()!];

        // References
        if (TryGetRef(description, out var path))
        {
            if (!path.StartsWith("#/definitions/")) throw new NotSupportedException($"the path {path} is not supported");
            var typeName = path.Split('/')[2];
            return this.TranslateTypeByName(typeName);
        }

        if (typeTag == "string" && TryGetEnum(description, out var members))
        {
            // Enumeration, declare
            var result = new Enum() { Name = GetDeclarationName() };
            Declare(result);

            // Extract members
            foreach (var (name, doc) in members)
            {
                var member = new EnumMember()
                {
                    Name = ToPascalCase(name),
                    Value = name,
                    Documentation = doc,
                };
                result.Members.Add(member);
            }

            return new DeclarationType(result);
        }

        if (typeTag == "array")
        {
            // Array type, translate element type
            var itemsDesc = description.GetProperty("items");
            var elementType = this.TranslateType(itemsDesc, nameHint: nameHint, parent: parent);
            return new ArrayType(elementType);
        }

        if (typeTag == "object")
        {
            // Regular class, declare
            var result = new Class() { Name = GetDeclarationName() };
            Declare(result);

            // Extract properties
            if (description.TryGetProperty("properties", out var props))
            {
                foreach (var propDesc in props.EnumerateObject())
                {
                    var prop = this.TranslateProperty(propDesc.Value, name: propDesc.Name, parent: result);
                    result.Properties.Add(prop);
                }
            }

            return new DeclarationType(result);
        }

        // Check for an array type-tag
        if (TryGetProperty(description, "type", out var type) && type.ValueKind == JsonValueKind.Array)
        {
            // This is a DU description
            return this.TranslateDuType(type.EnumerateArray().ToList(), nameHint: nameHint, parent: parent);
        }

        // Check for oneOf, just another way to specify DUs
        if (TryGetProperty(description, "oneOf", out var oneOf))
        {
            // This is a DU description
            return this.TranslateDuType(oneOf.EnumerateArray().ToList(), nameHint: nameHint, parent: parent);
        }

        // Check for allOf, which is essentially inheritance
        if (TryGetProperty(description, "allOf", out var allOf))
        {
            var elements = allOf.EnumerateArray().ToList();
            if (elements.Count != 2) throw new NotSupportedException($"the allOf type {nameHint} does not have 2 elements");

            // Translate both the base and derived types
            var baseType = UnwrapClass(this.TranslateType(elements[0], nameHint: null, parent: parent));
            var derivedType = UnwrapClass(this.TranslateType(elements[1], nameHint: nameHint, parent: parent));

            // Assign hierarchy
            derivedType.Base = baseType;
            return new DeclarationType(derivedType);
        }

        throw new NotImplementedException($"can not translate type {nameHint}: {description.GetRawText().Replace("\r\n", "")}");
    }

    private Type TranslateDuType(IReadOnlyList<JsonElement> descriptions, string? nameHint, Class? parent)
    {
        // A singleton is just that singular type
        if (descriptions.Count == 1) return this.TranslateType(descriptions[0], nameHint: nameHint, parent: parent);

        // If there are at least 7 elements, assume any type
        if (descriptions.Count >= 7) return this.translatedTypes["any"];

        // Check for a null string among the possibilities
        if (descriptions.Any(IsNullString))
        {
            // There is one, remove it
            descriptions = descriptions
                .Where(d => !IsNullString(d))
                .ToList();
            // Create a DU from the subset
            var subtype = this.TranslateDuType(descriptions, nameHint: nameHint, parent: parent);
            // Wrap in nullable
            return new NullableType(subtype);
        }

        // Just translate as-is
        var subtypes = descriptions
            .Select(d => this.TranslateType(d, nameHint: nameHint, parent: parent))
            .ToImmutableArray();
        return new DiscriminatedUnionType(subtypes);
    }

    private Property TranslateProperty(JsonElement description, string name, Class parent)
    {
        // Construct declaration
        var result = new Property()
        {
            Name = ToPascalCase(name),
            SerializedName = name,
        };
        ExtractDocumentation(description, result);

        // Translate type
        result.Type = this.TranslateType(description, nameHint: name, parent: parent);

        return result;
    }

    private static bool TryGetEnum(
        JsonElement element,
        [MaybeNullWhen(false)] out IReadOnlyList<(string Name, string? Doc)> members)
    {
        var docs = null as IReadOnlyList<string?>;
        if (element.TryGetProperty("enumDescriptions", out var enumDesc))
        {
            docs = enumDesc
                .EnumerateArray()
                .Select(e => e.GetString())
                .ToList();
        }
        if (element.TryGetProperty("enum", out var @enum))
        {
            docs ??= new string?[@enum.GetArrayLength()];
            members = @enum
                .EnumerateArray()
                .Zip(docs, (a, b) => (Name: a.GetString()!, Docs: b))
                .ToList();
            return true;
        }
        if (element.TryGetProperty("_enum", out var _enum))
        {
            docs ??= new string?[@enum.GetArrayLength()];
            members = _enum
                .EnumerateArray()
                .Zip(docs, (a, b) => (Name: a.GetString()!, Docs: b))
                .ToList();
            return true;
        }
        members = null;
        return false;
    }

    private static string? ExtractTypeTag(JsonElement element) =>
        TryGetProperty(element, "type", out var typeTag)
     && typeTag.ValueKind == JsonValueKind.String
        ? typeTag.GetString()
        : null;

    private static void ExtractDocumentation(JsonElement element, Declaration declaration)
    {
        if (element.TryGetProperty("title", out _)) { /* no-op*/ }
        if (element.TryGetProperty("description", out var doc)) declaration.Documentation = doc.GetString();
    }

    private static bool TryGetRef(JsonElement element, [MaybeNullWhen(false)] out string path)
    {
        if (TryGetProperty(element, "$ref", out var value))
        {
            path = value.GetString()!;
            return true;
        }
        else
        {
            path = null;
            return false;
        }
    }

    private static bool TryGetProperty(JsonElement element, string propName, out JsonElement prop)
    {
        if (element.ValueKind == JsonValueKind.Object) return element.TryGetProperty(propName, out prop);

        prop = default;
        return false;
    }

    private static bool IsNullString(JsonElement element) =>
           element.ValueKind == JsonValueKind.String
        && element.GetString() == "null";

    /// <summary>
    /// Translates a snake_case name to PascalCase.
    /// </summary>
    /// <param name="name">The name to translate.</param>
    /// <returns>The PascalCase version of <paramref name="name"/>.</returns>
    private static string ToPascalCase(string name)
    {
        var parts = name.Split('_');
        var result = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0) result.Append('_');
            else result.Append(Capitalize(part));
        }
        return result.ToString();
    }

    /// <summary>
    /// Extracts a prefix from a name, which is the first capitalized word in it.
    /// </summary>
    /// <param name="name">The name to extract the prefix from.</param>
    /// <returns>The first capitalized word in <paramref name="name"/>.</returns>
    private static string ExtractNamePrefix(string name)
    {
        // Search for the second uppercase letter
        var endIndex = 1;
        for (; endIndex < name.Length; ++endIndex)
        {
            var ch = name[endIndex];
            if (ch == char.ToUpper(ch)) break;
        }
        // Cut it off
        return name[..endIndex];
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
