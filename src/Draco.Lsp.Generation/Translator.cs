using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ts = Draco.Lsp.Generation.TypeScript;
using Cs = Draco.Lsp.Generation.CSharp;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Draco.Lsp.Generation;

/// <summary>
/// Translates a TypeScript object model to a C# one.
/// </summary>
internal sealed class Translator
{
    /// <summary>
    /// Represents the translation of a TypeScript type.
    /// </summary>
    private sealed class TypeTranslation
    {
        /// <summary>
        /// The original TypeScript element translated.
        /// </summary>
        public object? Original { get; set; }

        /// <summary>
        /// The primary C# output of the translation, like a class or enum.
        /// It is wrapped behind a type.
        /// </summary>
        public Cs.Type? Primary { get; set; }

        /// <summary>
        /// An additional translation, like an interface, if needed.
        /// </summary>
        public Cs.Interface? Interface { get; set; }
    }

    /// <summary>
    /// Searches for a direct type-reference.
    /// </summary>
    private sealed class TypeReferenceFinder : Ts.ModelVisitor
    {
        public static bool Find(Ts.Model model, string typeName)
        {
            var finder = new TypeReferenceFinder(typeName);
            finder.VisitModel(model);
            return finder.found;
        }

        private readonly string typeNameToSearch;
        private bool found;

        private TypeReferenceFinder(string typeNameToSearch)
        {
            this.typeNameToSearch = typeNameToSearch;
        }

        public override object? VisitNameExpression(Ts.NameExpression name)
        {
            this.found = this.found || name.Name == this.typeNameToSearch;
            return null;
        }

        // Don't cares
        public override object? VisitConstant(Ts.Constant c) => null;
        public override object? VisitNamespace(Ts.Namespace n) => null;
        public override object? VisitEnum(Ts.Enum e) => null;
        public override object? VisitIndexSignature(Ts.IndexSignature indexSign) => null;
    }

    /// <summary>
    /// The source TypeScript model.
    /// </summary>
    public Ts.Model SourceModel { get; }

    // The types we already generated
    private readonly Dictionary<string, TypeTranslation> translatedTypes = new();

    public Translator(Ts.Model sourceModel)
    {
        this.SourceModel = sourceModel;
    }

    /// <summary>
    /// Generates the model to everything that has been added so far.
    /// </summary>
    /// <returns>The generated model.</returns>
    public Cs.Model Generate()
    {
        var target = new Cs.Model();
        foreach (var translation in this.translatedTypes.Values)
        {
            if (translation.Primary is Cs.DeclarationType decl) target.Declarations.Add(decl.Declaration);
            if (translation.Interface is not null) target.Declarations.Add(translation.Interface);
        }
        return target;
    }

    /// <summary>
    /// Adds a builtin type that does not need translation anymore.
    /// </summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="type">The reflected type.</param>
    public void AddBuiltinType(string name, System.Type type) => this.translatedTypes.Add(name, new()
    {
        Primary = new Cs.BuiltinType(type),
    });

    /// <summary>
    /// Generates a type by its name.
    /// </summary>
    /// <param name="typeName">The name of the type to generate.</param>
    public void GenerateByName(string typeName)
    {
        if (this.TranslateByName(typeName) is null)
        {
            throw new ArgumentException($"could not find declaration for {typeName}", nameof(typeName));
        }
    }

    private TypeTranslation? TranslateByName(string typeName)
    {
        // If we already generated it, we are done
        if (this.translatedTypes.TryGetValue(typeName, out var existing)) return existing;
        // Search in the model
        if (!this.TryGetTsDeclarations(typeName, out var tsDecls)) return null;
        // Found one or more, see what fits
        // Simple
        if (tsDecls.Length == 1) return this.Translate(tsDecls[0]);
        // More complex
        if (tsDecls.Length == 2
         && tsDecls.OfType<Ts.TypeAlias>().Any()
         && tsDecls.OfType<Ts.Namespace>().Any())
        {
            // An enum with custom values
            var ta = tsDecls.OfType<Ts.TypeAlias>().First();
            var ns = tsDecls.OfType<Ts.Namespace>().First();
            return this.TranslateNamespace(ta, ns);
        }
        if (tsDecls.All(t => t is Ts.Interface))
        {
            // Multiple definitions of the same interface, find the largest subset
            var largestInterface = tsDecls
                .OfType<Ts.Interface>()
                .MaxBy(i => i.Fields.Length);
            if (largestInterface is null) return null;
            return this.Translate(largestInterface);
        }
        // Unhandled
        throw new NotImplementedException();
    }

    private TypeTranslation Translate(Ts.Declaration tsDecl) => tsDecl switch
    {
        Ts.Interface i => this.Translate(i),
        Ts.TypeAlias a => this.Translate(a),
        _ => throw new ArgumentOutOfRangeException(nameof(tsDecl)),
    };

    private TypeTranslation Translate(Ts.TypeAlias tsAlias)
    {
        var translation = new TypeTranslation()
        {
            Original = tsAlias,
        };
        this.translatedTypes.Add(tsAlias.Name, translation);

        var type = this.TranslateType(tsAlias.Type, null, hintName: tsAlias.Name);
        translation.Other = type;

        return translation;
    }

    private TypeTranslation Translate(Ts.Interface tsInterface)
    {
        var translation = new TypeTranslation()
        {
            Original = tsInterface,
        };
        this.translatedTypes.Add(tsInterface.Name, translation);

        // Check, if we need an interface
        // We need an interface, if any of the TS interfaces use it in inheritance
        var needsInterface = this.SourceModel.Declarations
            .OfType<Ts.Interface>()
            .Any(i => i.Bases.OfType<Ts.NameExpression>().Any(n => n.Name == tsInterface.Name));
        // Check, if we need a class
        // We need a class, if any of the fields reference the type directly
        var needsClass = TypeReferenceFinder.Find(this.SourceModel, tsInterface.Name);

        if (needsClass) this.TranslateClass(translation);
        if (needsInterface) this.TranslateInterface(translation, translation.Class!.NestedDeclarations);

        if (needsClass && needsInterface)
        {
            // The class implements the interface
            translation.Class!.Interfaces.Add(translation.Interface!);
        }

        if (needsClass)
        {
            // Add all interface props to the class
            var transitiveInterfaces = translation.Class!.Interfaces.SelectMany(CollectInterfacesTransitively);
            foreach (var prop in transitiveInterfaces.SelectMany(i => i.Properties))
            {
                if (translation.Class.Properties.Any(p => p.Name == prop.Name)) continue;
                translation.Class.Properties.Add(prop);
            }
        }

        return translation;
    }

    private void TranslateClass(TypeTranslation translation)
    {
        var tsInterface = (Ts.Interface)translation.Original;

        var csClass = new Cs.Class()
        {
            Documentation = ExtractDocumentation(tsInterface.Documentation),
            Name = tsInterface.Name,
        };
        translation.Class = csClass;

        // TODO: Generics
        if (tsInterface.GenericParams.Length > 0) throw new NotImplementedException();

        // Bases
        foreach (var b in tsInterface.Bases)
        {
            var interf = this.TranslateType(b, csClass.NestedDeclarations, needsInterface: true);
            csClass.Interfaces.Add((Cs.Interface)((Cs.DeclarationType)interf).Declaration);
        }

        var oldNameSuffix = this.nameSuffix;
        this.nameSuffix = ExtractNameSuffix(tsInterface.Name);

        // Fields
        foreach (var field in tsInterface.Fields)
        {
            var prop = this.TranslateField(field, csClass.NestedDeclarations);
            csClass.Properties.Add(prop);
        }

        this.nameSuffix = oldNameSuffix;
    }

    private void TranslateInterface(TypeTranslation translation, IList<Cs.Declaration> additionalDecls)
    {
        var tsInterface = (Ts.Interface)translation.Original;

        var csInterface = new Cs.Interface()
        {
            Documentation = ExtractDocumentation(tsInterface.Documentation),
            Name = $"I{tsInterface.Name}",
        };
        translation.Interface = csInterface;

        // TODO: Generics
        if (tsInterface.GenericParams.Length > 0) throw new NotImplementedException();

        // Bases
        foreach (var b in tsInterface.Bases)
        {
            var interf = this.TranslateType(b, null, needsInterface: true);
            csInterface.Interfaces.Add((Cs.Interface)((Cs.DeclarationType)interf).Declaration);
        }

        // Fields
        foreach (var field in tsInterface.Fields)
        {
            var prop = this.TranslateField(field, additionalDecls);
            csInterface.Properties.Add(prop);
        }
    }

    private TypeTranslation TranslateNamespace(Ts.TypeAlias alias, Ts.Namespace tsNamespace)
    {
        var translation = new TypeTranslation()
        {
            Original = tsNamespace,
        };
        this.translatedTypes.Add(tsNamespace.Name, translation);

        var csEnum = new Cs.Enum()
        {
            Documentation = ExtractDocumentation(tsNamespace.Documentation),
            Name = tsNamespace.Name,
            Members = tsNamespace.Constants.Select(this.TranslateEnumMember).ToList(),
        };
        translation.Other = new Cs.DeclarationType(csEnum);

        return translation;
    }

    private Cs.EnumMember TranslateEnumMember(Ts.Constant value)
    {
        if (value.Value is Ts.StringExpression str)
        {
            return new Cs.EnumMember(
                Documentation: ExtractDocumentation(value.Documentation),
                Name: Capitalize(value.Name),
                Attributes: ImmutableArray.Create(new Cs.Attribute(
                    Name: "JsonEnumName",
                    Args: ImmutableArray.Create<object?>(str.Value))));
        }
        else if (value.Value is Ts.IntExpression i)
        {
            return new Cs.EnumMember(
                Documentation: ExtractDocumentation(value.Documentation),
                Name: Capitalize(value.Name),
                Attributes: ImmutableArray.Create(new Cs.Attribute(
                    Name: "JsonEnumValue",
                    Args: ImmutableArray.Create<object?>(i.Value))));
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }
    }

    private Cs.Property TranslateField(Ts.Field field, IList<Cs.Declaration> additionalDecls)
    {
        if (field is Ts.SimpleField simpleField)
        {
            var propType = this.TranslateType(simpleField.Type, additionalDecls, hintName: simpleField.Name);
            return new Cs.Property(
                Documentation: ExtractDocumentation(simpleField.Documentation),
                Type: propType,
                Nullable: simpleField.Nullable,
                Name: Capitalize(simpleField.Name),
                Attributes: ImmutableArray.Create(new Cs.Attribute(
                    Name: "JsonPropertyName",
                    Args: ImmutableArray.Create<object?>(simpleField.Name))));
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }
    }

    private Cs.Type TranslateType(
        Ts.Expression type,
        IList<Cs.Declaration>? additionalDecls = null,
        string? hintName = null)
    {
        switch (type)
        {
        case Ts.NullExpression:
        {
            throw new InvalidOperationException();
        }
        case Ts.NameExpression name:
        {
            // If it is translated already, use it
            if (this.translatedTypes.TryGetValue(name.Name, out var builtin))
            {
                Debug.Assert(builtin.Primary is not null);
                return builtin.Primary;
            }
            // Translate by name
            var translation = this.TranslateByName(name.Name);
            if (translation is null) throw new InvalidOperationException($"could not translate type named {name.Name}");
            if (translation.Primary is null) throw new InvalidOperationException($"could not translate type named {name.Name}");
            return translation.Primary;
        }
        case Ts.UnionTypeExpression union:
        {
            // If the union is only a single element, unwrap it
            if (union.Alternatives.Length == 1)
            {
                return this.TranslateType(union.Alternatives[0], additionalDecls, hintName);
            }
            // If the union has a null element, make an optional type of it
            if (union.Alternatives.Any(alt => alt is Ts.NullExpression))
            {
                // Filter out the non-null elements
                var alts = union.Alternatives
                    .Where(alt => alt is not Ts.NullExpression)
                    .ToImmutableArray();
                var newUnion = new Ts.UnionTypeExpression(alts);
                // Translate that
                var subType = this.TranslateType(newUnion, additionalDecls, hintName);
                // Wrap it up in nullable
                return new Cs.NullableType(subType);
            }
            // Union of anonymous expressions
            if (union.Alternatives.All(alt => alt is Ts.AnonymousTypeExpression))
            {
                // We just find a common denominator by merging
                var alts = union.Alternatives.Cast<Ts.AnonymousTypeExpression>();
                var newType = MergeAnonymousTypes(alts);
                return this.TranslateType(newType, additionalDecls, hintName);
            }
            // Not a special case, just translate
            var elements = union.Alternatives
                .Select(alt => this.TranslateType(alt, additionalDecls, hintName))
                .Select(translation => translation.Primary)
                .ToImmutableArray();
            return new Cs.DiscriminatedUnionType(elements);
        }
        }
        if (type is Ts.UnionTypeExpression union)
        {
            // Union of anything and a null
            if (union.Alternatives.Any(alt => alt is Ts.NameExpression { Name: "null" }))
            {
                // We take out the null
                var remAlts = union.Alternatives
                    .Where(alt => alt is not Ts.NameExpression { Name: "null" })
                    .ToImmutableArray();
                if (remAlts.Length == 1)
                {
                    // It's a single alternative from now
                    var single = this.TranslateType(remAlts[0], additionalDecls, hintName: hintName);
                    return new Cs.NullableType(single);
                }
                else
                {
                    // Still multiple
                    var multi = this.TranslateType(new Ts.UnionTypeExpression(remAlts), additionalDecls);
                    return new Cs.NullableType(multi);
                }
            }

            // Union of anonymous expressions
            if (union.Alternatives.All(alt => alt is Ts.AnonymousTypeExpression))
            {
                // We just find a common denominator by merging
                var newType = MergeAnonymousTypes(union.Alternatives.Cast<Ts.AnonymousTypeExpression>());
                return this.TranslateType(newType, additionalDecls, hintName: hintName);
            }

            
        }
        else if (type is Ts.ArrayTypeExpression array)
        {
            var nextHint = hintName is null
                ? null
                : Singular(hintName);
            var element = this.TranslateType(array.ElementType, additionalDecls, hintName: nextHint);
            return new Cs.ArrayType(element);
        }
        else if (type is Ts.AnonymousTypeExpression anon)
        {
            if (anon.Fields.Length == 1 && anon.Fields[0] is Ts.IndexSignature indexSignature)
            {
                // Shortcut, just translate it to a dictionary
                var keyType = this.TranslateType(indexSignature.KeyType, additionalDecls, hintName: indexSignature.KeyName);
                var valueType = this.TranslateType(indexSignature.ValueType, additionalDecls);
                return new Cs.DictionaryType(keyType, valueType);
            }

            Debug.Assert(hintName is not null);
            var typeNamePrefix = Capitalize(hintName);
            var typeName = $"{typeNamePrefix}{this.nameSuffix}";
            // Look for it in the additional decls
            var existing = additionalDecls?.FirstOrDefault(decl => decl.Name.StartsWith(typeNamePrefix));
            if (existing is not null) return new Cs.DeclarationType(existing);
            // Not found, we need to translate
            var csClass = new Cs.Class()
            {
                Documentation = null,
                Name = typeName,
            };
            // Translate fields
            foreach (var field in anon.Fields)
            {
                var prop = this.TranslateField(field, csClass.NestedDeclarations);
                csClass.Properties.Add(prop);
            }
            // Add declaration
            if (additionalDecls is null)
            {
                var translation = new TypeTranslation()
                {
                    Original = anon,
                    Class = csClass,
                };
                this.translatedTypes.Add(csClass.Name, translation);
            }
            else
            {
                additionalDecls.Add(csClass);
            }
            // We can return it wrapped up as a type reference
            return new Cs.DeclarationType(csClass);
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }
    }

    private bool TryGetTsDeclarations(string name, out ImmutableArray<Ts.Declaration> decls)
    {
        decls = this.SourceModel.Declarations
            .Where(d => d.Name == name)
            .ToImmutableArray();
        return decls.Length != 0;
    }

    /// <summary>
    /// Collects all transitive base interfaces.
    /// </summary>
    /// <param name="interface">The interface to get the bases of.</param>
    /// <returns>The sequence of all base interfaces of <paramref name="interface"/>.</returns>
    private static IEnumerable<Cs.Interface> CollectInterfacesTransitively(Cs.Interface @interface)
    {
        yield return @interface;
        foreach (var b in @interface.Interfaces.SelectMany(CollectInterfacesTransitively)) yield return b;
    }

    /// <summary>
    /// Merges multiple anonymous types into one.
    /// </summary>
    /// <param name="alternatives">The alternative anonymous types to merge.</param>
    /// <returns>A single type, containing all fields of the types.</returns>
    private static Ts.Expression MergeAnonymousTypes(IEnumerable<Ts.AnonymousTypeExpression> alternatives)
    {
        var newFields = new Dictionary<string, Ts.SimpleField>();
        foreach (var alt in alternatives)
        {
            foreach (var field in alt.Fields.Cast<Ts.SimpleField>())
            {
                if (!newFields.TryGetValue(field.Name, out var existing))
                {
                    // New, add it
                    newFields.Add(field.Name, field);
                }
                else
                {
                    // We need to compare
                    // Types must equal
                    if (!Equals(field.Type, existing.Type)) throw new InvalidOperationException();
                    // Check, if nullability is looser
                    // If so, replace
                    if (field.Nullable && !existing.Nullable) newFields[field.Name] = field;
                }
            }
        }
        return new Ts.AnonymousTypeExpression(newFields.Values.Cast<Ts.Field>().ToImmutableArray());
    }

    /// <summary>
    /// Capitalizes a word.
    /// </summary>
    /// <param name="word">The word to capitalize.</param>
    /// <returns>The capitalized <paramref name="word"/>.</returns>
    private static string Capitalize(string word) => $"{char.ToUpper(word[0])}{word[1..]}";

    /// <summary>
    /// Converts a word to singular.
    /// </summary>
    /// <param name="word">The word to convert.</param>
    /// <returns><paramref name="word"/> in singular form.</returns>
    private static string Singular(string word) => word.EndsWith('s') ? word[..^1] : word;

    /// <summary>
    /// Extracts a suffix from a name, which is the last capitalized word in it.
    /// </summary>
    /// <param name="name">The name to extract the suffix from.</param>
    /// <returns>The last capitalized word in <paramref name="name"/>.</returns>
    private static string ExtractNameSuffix(string name)
    {
        var upcaseLen = name
            .Reverse()
            .TakeWhile(c => c != char.ToUpperInvariant(c))
            .Count() + 1;
        return name[^upcaseLen..];
    }

    /// <summary>
    /// Extracts documentation from a TypeScript comment.
    /// </summary>
    /// <param name="docComment">The TS comment to extract docs from.</param>
    /// <returns>The extracted documentation text or null, if <paramref name="docComment"/> was deemed not to
    /// be a doc comment.</returns>
    private static string? ExtractDocumentation(string? docComment)
    {
        if (docComment is null) return null;

        // Normalize newlines
        docComment = docComment
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');
        // Split into lines
        var lines = docComment.Split('\n');
        if (lines.Length < 3)
        {
            if (lines.Length == 1)
            {
                // Could be /** ... */
                var line = lines[0].Trim();
                if (line.StartsWith("/**") && line.EndsWith("*/")) return line[3..^2].Trim();
                // Unknown, discard
                return null;
            }
            // TODO
            throw new NotImplementedException();
        }
        // At least 3 lines, check for JavaDoc notation
        if (lines[0].Trim() != "/**" || lines[^1].Trim() != "*/") return null;
        // Ok
        var trimmedLines = lines[1..^1]
            .Select(line => line[(line.IndexOf('*') + 1)..].Trim());
        return string.Join(Environment.NewLine, trimmedLines);
    }
}
