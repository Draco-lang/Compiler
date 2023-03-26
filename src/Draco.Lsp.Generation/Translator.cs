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

internal sealed class Translator
{
    private sealed class TypeTranslation
    {
        public object Original { get; set; } = null!;
        public Cs.Class? Class { get; set; }
        public Cs.Interface? Interface { get; set; }
        public Cs.Type? Other { get; set; }
    }

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

    /// <summary>
    /// The model being built.
    /// </summary>
    public Cs.Model TargetModel { get; } = new();

    // The types we already generated
    private readonly Dictionary<string, TypeTranslation> translatedTypes = new();
    // Builtins
    private readonly Dictionary<string, Cs.Type> builtinTypes = new();

    private string? nameSuffix;

    public Translator(Ts.Model sourceModel)
    {
        this.SourceModel = sourceModel;
    }

    public void Commit()
    {
        // Declarations
        foreach (var translation in this.translatedTypes.Values)
        {
            if (translation.Class is not null) this.TargetModel.Declarations.Add(translation.Class);
            if (translation.Interface is not null) this.TargetModel.Declarations.Add(translation.Interface);
            if (translation.Other is Cs.DeclarationType dt)
            {
                // Skip duplicates
                // if (this.translatedTypes.ContainsKey(dt.Declaration.Name)) continue;
                this.TargetModel.Declarations.Add(dt.Declaration);
            }
        }
    }

    public void AddBuiltinType(string name, System.Type type) => this.builtinTypes.Add(name, new Cs.BuiltinType(type));

    public void GenerateByName(string typeName)
    {
        if (this.TranslateByName(typeName) is null)
        {
            Console.WriteLine($"Could not find declaration for {typeName}");
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
            var largestInterface = tsDecls
                .OfType<Ts.Interface>()
                .MaxBy(i => i.Fields.Length);
            return this.Translate(largestInterface!);
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
        IList<Cs.Declaration>? additionalDecls,
        bool needsInterface = false,
        string? hintName = null)
    {
        if (type is Ts.NameExpression name)
        {
            // If it's a builtin, use that
            if (this.builtinTypes.TryGetValue(name.Name, out var builtin)) return builtin;
            // Translate by name
            var translation = this.TranslateByName(name.Name);
            if (needsInterface)
            {
                var iDecl = translation?.Interface ?? throw new InvalidOperationException();
                return new Cs.DeclarationType(iDecl);
            }
            var typeRef = translation?.Class is null
                    ? translation?.Other
                    : new Cs.DeclarationType(translation.Class);
            return typeRef is null
                ? throw new InvalidOperationException()
                : typeRef;
        }
        else if (type is Ts.UnionTypeExpression union)
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
                var newType = MergeAnonymousAlternatives(union.Alternatives.Cast<Ts.AnonymousTypeExpression>());
                return this.TranslateType(newType, additionalDecls, hintName: hintName);
            }

            var elements = union.Alternatives
                .Select(alt => this.TranslateType(alt, additionalDecls, hintName: hintName))
                .ToImmutableArray();
            return new Cs.DiscriminatedUnionType(elements);
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

    private static IEnumerable<Cs.Interface> CollectInterfacesTransitively(Cs.Interface @interface)
    {
        yield return @interface;
        foreach (var b in @interface.Interfaces.SelectMany(CollectInterfacesTransitively)) yield return b;
    }

    private static Ts.Expression MergeAnonymousAlternatives(IEnumerable<Ts.AnonymousTypeExpression> alternatives)
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

    private static string Capitalize(string s) => $"{char.ToUpper(s[0])}{s[1..]}";

    private static string Singular(string s) => s.EndsWith('s')
        ? s[..^1]
        : s;

    private static string ExtractNameSuffix(string name)
    {
        var upcaseLen = name
            .Reverse()
            .TakeWhile(c => c != char.ToUpperInvariant(c))
            .Count() + 1;
        return name[^upcaseLen..];
    }

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
