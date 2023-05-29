using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using Draco.SourceGeneration.Dap.CsModel;

namespace Draco.SourceGeneration.Dap;

/// <summary>
/// Translates the JSON schema to a C# model.
/// </summary>
internal sealed class Translator
{
    private readonly JsonDocument sourceModel;
    private readonly Model targetModel = new();
    private readonly Dictionary<string, Class> translatedTypes = new();

    public Translator(JsonDocument sourceModel)
    {
        this.sourceModel = sourceModel;
    }

    public Model Translate()
    {
        var types = this.sourceModel.RootElement
            .GetProperty("definitions")
            .EnumerateObject();
        foreach (var prop in types) this.TranslateByPath($"#/definitions/{prop.Name}");

        return this.targetModel;
    }

    private Class TranslateByPath(string path)
    {
        if (this.translatedTypes.TryGetValue(path, out var existing)) return existing;

        if (!path.StartsWith("#/definitions/")) throw new ArgumentException($"only definition references are supported, {path} is not one");
        var pathParts = path.Split('/');
        if (pathParts.Length != 3) throw new ArgumentException($"path {path} does not have exactly 3 nesting");

        var typeName = pathParts[^1];
        var typeToTranslate = this.sourceModel.RootElement
            .GetProperty("definitions")
            .EnumerateObject()
            .FirstOrDefault(prop => prop.Name == typeName);
        if (typeToTranslate.Name is null) throw new KeyNotFoundException($"the type {typeName} could not be found for translation");

        var target = new Class();
        target.Name = typeName;
        this.translatedTypes.Add(path, target);
        this.targetModel.Classes.Add(target);
        this.TranslateType(typeToTranslate.Value, target);
        return target;
    }

    private void TranslateType(JsonElement sourceType, Class targetType)
    {
        if (sourceType.ValueKind != JsonValueKind.Object) throw new ArgumentException($"definition {targetType.Name} is not an object");

        if (sourceType.TryGetProperty("allOf", out var elements))
        {
            var toImplement = elements.EnumerateArray().ToList();
            if (toImplement.Count != 2) throw new NotSupportedException($"definition {targetType.Name} does not mix in 2 types");

            // Base class
            if (!TryGetRef(toImplement[0], out var path)) throw new NotSupportedException($"definition {targetType.Name} uses allOf, but does not mix in a type as its first element");
            targetType.Base = this.TranslateByPath(path);

            // Mixin
            this.TranslateType(toImplement[1], targetType);
        }
        else
        {
            ExtractDocumentation(sourceType, targetType);

            if (sourceType.TryGetProperty("properties", out var props))
            {
                foreach (var prop in props.EnumerateObject())
                {
                    var targetProp = new Property();
                    targetProp.Name = Capitalize(prop.Name);
                    this.TranslateProperty(prop.Value, targetProp);
                    targetType.Properties.Add(targetProp);
                }
            }
        }
    }

    private void TranslateProperty(JsonElement sourceProperty, Property targetProperty)
    {
        ExtractDocumentation(sourceProperty, targetProperty);

        // Determine type
        if (sourceProperty.TryGetProperty("type", out var type))
        {
            // TODO
            targetProperty.Type = new BuiltinType($"Unknown<{type}>");
        }
        else if (sourceProperty.TryGetProperty("$ref", out var @ref))
        {
            var path = @ref.GetString()!;
            var refClass = this.TranslateByPath(path);
            targetProperty.Type = new DeclarationType(refClass);
        }
        else if (sourceProperty.TryGetProperty("oneOf", out var variants))
        {
            // TODO
            targetProperty.Type = new BuiltinType($"UnknownOneOf<{type}>");
        }
        else
        {
            throw new ArgumentException($"could not determine the type of property {targetProperty.Name}");
        }
    }

    private static bool TryGetRef(JsonElement element, [MaybeNullWhen(false)] out string path)
    {
        if (element.TryGetProperty("$ref", out var value))
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

    private static void ExtractDocumentation(JsonElement element, Declaration declaration)
    {
        if (element.TryGetProperty("title", out _)) { /* no-op*/ }
        if (element.TryGetProperty("description", out var doc)) declaration.Documentation = doc.GetString();
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
