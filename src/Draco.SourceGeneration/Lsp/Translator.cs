using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Ts = Draco.SourceGeneration.Lsp.TypeScript;
using Cs = Draco.SourceGeneration.Lsp.CSharp;

namespace Draco.SourceGeneration.Lsp;

/// <summary>
/// Translates a TypeScript object model to a C# one.
/// </summary>
internal sealed class Translator
{
    /// <summary>
    /// Searches for a direct type-reference.
    /// </summary>
    private sealed class TypeReferenceFinder : Ts.ModelVisitor
    {
        /// <summary>
        /// Looks up of a type name is referenced directly inside a model.
        /// </summary>
        /// <param name="model">The model to search in.</param>
        /// <param name="typeName">The type name to search for.</param>
        /// <returns>True, if <paramref name="model"/> references <paramref name="typeName"/> directly.</returns>
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

    // The types we already translated
    private readonly Dictionary<string, Cs.Type> translatedTypes = new();
    // Interfaces in addition to the base types, in case it is needed
    private readonly Dictionary<Cs.Type, Cs.Interface> interfaces = new(ReferenceEqualityComparer.Instance);

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
        foreach (var decl in this.translatedTypes.Values.OfType<Cs.DeclarationType>())
        {
            target.Declarations.Add(decl.Declaration);
            if (this.interfaces.TryGetValue(decl, out var @interface)) target.Declarations.Add(@interface);
        }
        return target;
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
    /// Generates a type by its name.
    /// </summary>
    /// <param name="typeName">The name of the type to generate.</param>
    public void GenerateByName(string typeName) =>
        this.TranslateTypeByDeclarationName(typeName);

    /// <summary>
    /// Translates a type by its declaration name in the TS model.
    /// </summary>
    /// <param name="typeName">The name of the declared TS type.</param>
    /// <returns>The translated C# type.</returns>
    private Cs.Type TranslateTypeByDeclarationName(string typeName)
    {
        // If we already generated it, we are done
        if (this.translatedTypes.TryGetValue(typeName, out var existing)) return existing;

        // Search in the model
        if (!this.TryGetTsDeclarations(typeName, out var tsDecls))
        {
            throw new ArgumentException($"could not find declaration with name {typeName}", nameof(typeName));
        }

        // Now depending on what we have, we have different cases
        if (tsDecls.Length == 1)
        {
            // Simple case, we only have one declaration
            return tsDecls[0] switch
            {
                Ts.TypeAlias alias => this.TranslateTypeAlias(alias),
                Ts.Interface @interface => this.TranslateInterface(@interface),
                _ => throw new NotImplementedException(),
            };
        }
        if (tsDecls.Length == 2 && tsDecls.Any(d => d is Ts.TypeAlias) && tsDecls.Any(d => d is Ts.Namespace))
        {
            // An enum with custom values
            var typeAlias = tsDecls.OfType<Ts.TypeAlias>().First();
            var @namespace = tsDecls.OfType<Ts.Namespace>().First();
            return this.TranslateEnum(@namespace);
        }
        if (tsDecls.All(d => d is Ts.Interface))
        {
            // Multiple definitions of the same interface, find the largest subset
            var largestInterface = tsDecls
                .Cast<Ts.Interface>()
                .MaxBy(i => i.Fields.Length);
            return this.TranslateInterface(largestInterface);
        }
        // Unhandled
        throw new NotImplementedException();
    }

    /// <summary>
    /// Translated the given TS type-alias.
    /// </summary>
    /// <param name="tsAlias">The TS type-alias to translate.</param>
    /// <returns>The translated, aliased type.</returns>
    private Cs.Type TranslateTypeAlias(Ts.TypeAlias tsAlias)
    {
        var type = this.TranslateType(tsAlias.Type, nameHint: tsAlias.Name, containingClass: null);
        if (this.translatedTypes.TryGetValue(tsAlias.Name, out var existingType))
        {
            // Happens on things like
            // type Foo = { ... };
            // because the anonymous type gets hinted the type-alias name
            if (!ReferenceEquals(type, existingType)) throw new InvalidOperationException();
        }
        else
        {
            // Add it
            this.translatedTypes.Add(tsAlias.Name, type);
        }
        return type;
    }

    /// <summary>
    /// Translated the given TS interface.
    /// </summary>
    /// <param name="tsInterface">The TS interface to translate.</param>
    /// <returns>The equivalent C# type.</returns>
    private Cs.Type TranslateInterface(Ts.Interface tsInterface)
    {
        // An interface will either produce a class, an interface, or both depending on usage
        var csClass = null as Cs.Class;
        var csInterface = null as Cs.Interface;

        // We need a class, if any of the fields reference the type directly
        var needsClass = TypeReferenceFinder.Find(this.SourceModel, tsInterface.Name);
        // We need an interface, if any of the TS interfaces use it in inheritance
        var needsInterface = this.SourceModel.Declarations
            .OfType<Ts.Interface>()
            .Any(i => i.Bases.OfType<Ts.NameExpression>().Any(n => n.Name == tsInterface.Name));
        // In case we don't make an interface, we do make a class
        if (!needsInterface) needsClass = true;

        // Instantiate whichever we need
        if (needsClass)
        {
            csClass = new Cs.Class()
            {
                Name = tsInterface.Name,
                Documentation = ExtractDocumentation(tsInterface.Documentation),
            };
        }
        if (needsInterface)
        {
            csInterface = new Cs.Interface()
            {
                Name = $"I{tsInterface.Name}",
                Documentation = ExtractDocumentation(tsInterface.Documentation),
            };
        }

        // Register early in case of recursion
        var typeReference =
            csClass is not null ? new Cs.DeclarationType(csClass) :
            csInterface is not null ? new Cs.DeclarationType(csInterface) :
            throw new InvalidOperationException();
        this.translatedTypes.Add(tsInterface.Name, typeReference);

        // TODO: Generics
        if (tsInterface.GenericParams.Length > 0) throw new NotImplementedException();

        // Add base types
        foreach (var baseExpr in tsInterface.Bases)
        {
            var @base = this.TranslateType(baseExpr, nameHint: null, containingClass: null);
            // We expect an interface alternative to be present
            var @interface = this.interfaces[@base];

            // Add to whichever needs it
            if (csClass is not null) csClass.Interfaces.Add(@interface);
            if (csInterface is not null) csInterface.Interfaces.Add(@interface);
        }

        // Add fields
        foreach (var field in tsInterface.Fields)
        {
            var prop = this.TranslateField(field, containingClass: csClass);
            // Add to whichever needs it
            if (csClass is not null) csClass.Properties.Add(prop);
            if (csInterface is not null) csInterface.Properties.Add(prop);
        }

        // Map the output to the interface, in case there is one
        if (csInterface is not null) this.interfaces.Add(typeReference, csInterface);

        // If we have a class, transitively implement all interfaces
        if (csClass is not null)
        {
            var allInterfaces = csClass.Interfaces.SelectMany(CollectInterfacesTransitively);
            foreach (var prop in allInterfaces.SelectMany(i => i.Properties))
            {
                // If the class happens to implement it, skip it
                if (csClass.Properties.Any(p => p.Name == prop.Name)) continue;
                // Add it
                csClass.Properties.Add(prop);
            }
        }
        // If we have a class, initialize parentship
        if (csClass is not null) csClass.InitializeParents();

        // Done
        return typeReference;
    }

    /// <summary>
    /// Translates the given TS namespace to a C# enum.
    /// </summary>
    /// <param name="tsNamespace">The TS namespace to translate.</param>
    /// <returns>The equivalent C# type.</returns>
    private Cs.Type TranslateEnum(Ts.Namespace tsNamespace)
    {
        var csEnum = new Cs.Enum()
        {
            Documentation = ExtractDocumentation(tsNamespace.Documentation),
            Name = tsNamespace.Name,
            Members = tsNamespace.Constants.Select(this.TranslateEnumMember).ToList(),
        };
        var typeRef = new Cs.DeclarationType(csEnum);
        this.translatedTypes.Add(tsNamespace.Name, typeRef);
        return typeRef;
    }

    /// <summary>
    /// Translates a contant to an enum member.
    /// </summary>
    /// <param name="constant">The constant to translate.</param>
    /// <returns>The translated enum member.</returns>
    private Cs.EnumMember TranslateEnumMember(Ts.Constant constant)
    {
        // Extract constant value
        var value = this.EvaluateExpression(constant.Value);
        // Create member
        return new(
            Documentation: ExtractDocumentation(constant.Documentation),
            Name: Capitalize(constant.Name),
            SerializedValue: value);
    }

    /// <summary>
    /// Evaluates a TS expression to a constant.
    /// </summary>
    /// <param name="expr">The expression to evaluate.</param>
    /// <returns>The evaluation result.</returns>
    private object EvaluateExpression(Ts.Expression expr) => expr switch
    {
        Ts.StringExpression s => s.Value,
        Ts.IntExpression i => i.Value,
        _ => throw new ArgumentOutOfRangeException(nameof(expr)),
    };

    /// <summary>
    /// Translated a TS field declaration.
    /// </summary>
    /// <param name="field">The field declaration to translate.</param>
    /// <param name="containingClass">The containing type.</param>
    /// <returns>The translated property.</returns>
    private Cs.Property TranslateField(Ts.Field field, Cs.Class? containingClass)
    {
        switch (field)
        {
        case Ts.SimpleField simpleField:
        {
            // Translate type
            var propType = this.TranslateType(
                simpleField.Type,
                nameHint: simpleField.Name,
                containingClass: containingClass);
            if (simpleField.Nullable)
            {
                // If the type is not nullable, we make it one
                if (propType is not Cs.NullableType) propType = new Cs.NullableType(propType);
            }
            // Finally we can create the property
            return new(
                Documentation: ExtractDocumentation(simpleField.Documentation),
                Name: Capitalize(simpleField.Name),
                Type: propType,
                SerializedName: simpleField.Name,
                OmitIfNull: simpleField.Nullable,
                IsExtensionData: false);
        }
        case Ts.IndexSignature indexSignature:
        {
            // We just translate to a catch-all
            return new(
                Documentation: ExtractDocumentation(indexSignature.Documentation),
                Name: "Extra",
                // NOTE: While the index signature has an exact type, this is simpler
                // Because Newtonsoft has direct support for this with JsonExtensionData
                Type: new Cs.DictionaryType(
                    new Cs.BuiltinType(typeof(string).FullName),
                    new Cs.BuiltinType(typeof(object).FullName)),
                // Serialized name does not matter
                SerializedName: string.Empty,
                OmitIfNull: true,
                IsExtensionData: true);
        }
        default:
            throw new ArgumentOutOfRangeException(nameof(field));
        }
    }

    /// <summary>
    /// Translates a TS type to C#.
    /// </summary>
    /// <param name="type">The TS type expression to translate.</param>
    /// <param name="nameHint">A hint for name.</param>
    /// <param name="containingClass">The containing class.</param>
    /// <returns>The translated C# type.</returns>
    private Cs.Type TranslateType(Ts.Expression type, string? nameHint, Cs.Class? containingClass)
    {
        string GenerateName()
        {
            if (nameHint is null) throw new InvalidOperationException("a hint is required to generate type-name");
            var typeName = Capitalize(nameHint);
            if (containingClass is not null) typeName = $"{typeName}{ExtractNameSuffix(containingClass.Name)}";
            return typeName;
        }

        Cs.Type AddLocalType(Cs.Declaration decl)
        {
            var typeRef = new Cs.DeclarationType(decl);
            // Add as a translated type or as a nested type
            if (containingClass is not null) containingClass.NestedDeclarations.Add(decl);
            else this.translatedTypes.Add(decl.Name, typeRef);
            return typeRef;
        }

        switch (type)
        {
        case Ts.NameExpression nameExpr:
        {
            // If it is translated already, use it
            if (this.translatedTypes.TryGetValue(nameExpr.Name, out var existing)) return existing;
            // Not translated, we have to translate by name
            return this.TranslateTypeByDeclarationName(nameExpr.Name);
        }
        case Ts.ArrayTypeExpression array:
        {
            var element = this.TranslateType(array.ElementType, nameHint: Singular(nameHint), containingClass: containingClass);
            return new Cs.ArrayType(element);
        }
        case Ts.UnionTypeExpression union:
        {
            // If the union consists of a single type, just propagate
            if (union.Alternatives.Length == 1)
            {
                return this.TranslateType(union.Alternatives[0], nameHint: nameHint, containingClass: containingClass);
            }
            // If the union has a null-element, make it instead an optional type
            if (union.Alternatives.Any(alt => alt is Ts.NullExpression))
            {
                // Filter out the non-null elements
                var alts = union.Alternatives
                    .Where(alt => alt is not Ts.NullExpression)
                    .ToImmutableArray();
                var newUnion = new Ts.UnionTypeExpression(alts);
                // Translate that
                var subType = this.TranslateType(newUnion, nameHint: nameHint, containingClass: containingClass);
                // Wrap it up in nullable
                return new Cs.NullableType(subType);
            }
            // If we union together anonymous expressions, we can merge the anonymous expressions
            if (union.Alternatives.All(alt => alt is Ts.AnonymousTypeExpression))
            {
                var alts = union.Alternatives.Cast<Ts.AnonymousTypeExpression>();
                var newType = MergeAnonymousTypes(alts);
                return this.TranslateType(newType, nameHint: nameHint, containingClass: containingClass);
            }
            // If we union together string values, it is an enumeration
            if (union.Alternatives.All(alt => alt is Ts.StringExpression))
            {
                var strAlts = union.Alternatives
                    .Cast<Ts.StringExpression>()
                    .Select(str => str.Value);
                // Generate a name
                var typeName = GenerateName();
                // Generate members
                var csEnum = new Cs.Enum()
                {
                    Name = typeName,
                    Members = strAlts
                        .Select(str => new Cs.EnumMember(
                            Documentation: null,
                            Name: Capitalize(str),
                            SerializedValue: str))
                        .ToList(),
                };
                // Register it
                return AddLocalType(csEnum);
            }
            // Not a special case, just translate
            var elements = union.Alternatives
                .Select(alt => this.TranslateType(alt, nameHint: nameHint, containingClass: containingClass))
                .ToImmutableArray();
            return new Cs.DiscriminatedUnionType(elements);
        }
        case Ts.AnonymousTypeExpression anon:
        {
            // Special-case, if the anonymous type contains a single index signature
            if (anon.Fields.Length == 1 && anon.Fields[0] is Ts.IndexSignature indexSignature)
            {
                // Shortcut, just translate it to a dictionary
                var keyType = this.TranslateType(indexSignature.KeyType, nameHint: indexSignature.KeyName, containingClass: containingClass);
                var valueType = this.TranslateType(indexSignature.ValueType, nameHint: null, containingClass: containingClass);
                return new Cs.DictionaryType(keyType, valueType);
            }

            // Not a special case
            if (nameHint is null) throw new InvalidOperationException("a hint is required to generate anonymous types");

            // Generate a name for it
            var typeName = GenerateName();
            // NOTE: Should we check if this already exists?
            // Probably not
            // Translate it
            var csClass = new Cs.Class()
            {
                Name = typeName,
            };
            // No base type or generics
            // Translate fields
            foreach (var field in anon.Fields)
            {
                var prop = this.TranslateField(field, containingClass);
                csClass.Properties.Add(prop);
            }
            // Register it
            return AddLocalType(csClass);
        }
        default:
            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    /// <summary>
    /// Retrieves the declarations from the TS code with a given name.
    /// </summary>
    /// <param name="name">The name to retrieve by.</param>
    /// <param name="decls">The declarations with name <paramref name="name"/> get written here.</param>
    /// <returns>True, if at least one declaration was found with <paramref name="name"/>.</returns>
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
    [return: NotNullIfNotNull(nameof(word))]
    private static string? Capitalize(string? word) => word is null
        ? null
        : $"{char.ToUpper(word[0])}{word.Substring(1)}";

    /// <summary>
    /// Converts a word to singular.
    /// </summary>
    /// <param name="word">The word to convert.</param>
    /// <returns><paramref name="word"/> in singular form.</returns>
    [return: NotNullIfNotNull(nameof(word))]
    private static string? Singular(string? word)
    {
        if (word is null) return null;
        return word.EndsWith("s")
            ? word.Substring(0, word.Length - 1)
            : word;
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
        return name.Substring(startIndex);
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
                if (line.StartsWith("/**") && line.EndsWith("*/"))
                {
                    return line.Substring(3, line.Length - 5).Trim();
                }
                // Unknown, discard
                return null;
            }
            // TODO
            throw new NotImplementedException();
        }
        // At least 3 lines, check for JavaDoc notation
        if (lines[0].Trim() != "/**" || lines[lines.Length - 1].Trim() != "*/") return null;
        // Ok
        var trimmedLines = lines
            // These 2 lines are cutting of the first and last lines, we do this because ns2.0
            .Skip(1)
            .Take(lines.Length - 2)
            // Cut off leading star
            .Select(line => line.Substring(line.IndexOf('*') + 1).Trim());
        return string.Join("\n", trimmedLines);
    }
}
