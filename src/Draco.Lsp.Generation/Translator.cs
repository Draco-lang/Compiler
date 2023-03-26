using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ts = Draco.Lsp.Generation.TypeScript;
using Cs = Draco.Lsp.Generation.CSharp;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;

namespace Draco.Lsp.Generation;

internal sealed class Translator
{
    private sealed class TypeTranslation
    {
        public Ts.Declaration Original { get; set; } = null!;
        public Cs.Class? Class { get; set; }
        public Cs.Interface? Interface { get; set; }
        public Cs.Declaration? Other { get; set; }
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

    public Translator(Ts.Model sourceModel)
    {
        this.SourceModel = sourceModel;
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
        _ => throw new ArgumentOutOfRangeException(nameof(tsDecl)),
    };

    private TypeTranslation Translate(Ts.Interface tsInterface)
    {
        var translation = new TypeTranslation()
        {
            Original = tsInterface,
        };
        this.translatedTypes.Add(tsInterface.Name, translation);

        // Check, if we need an interface
        // We need an interface, if any of the TS interfaces use it in multi-inheritance
        var needsInterface = this.SourceModel.Declarations
            .OfType<Ts.Interface>()
            .Any(i => i.Bases.Length > 1
                   && i.Bases.OfType<Ts.NameExpression>().Any(n => n.Name == tsInterface.Name));
        // Check, if we need a class
        // We need a class, if any of the fields reference the type directly
        var needsClass = TypeReferenceFinder.Find(this.SourceModel, tsInterface.Name);

        if (needsInterface) this.TranslateInterface(translation);
        if (needsClass) this.TranslateClass(translation);

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

        // TODO: Bases
        if (tsInterface.Bases.Length > 0) throw new NotImplementedException();

        // Fields
        foreach (var field in tsInterface.Fields)
        {
            var prop = this.TranslateField(field, csClass.NestedDeclarations);
            csClass.Properties.Add(prop);
        }
    }

    private void TranslateInterface(TypeTranslation translation)
    {
        var tsInterface = (Ts.Interface)translation.Original;

        var csInterface = new Cs.Interface()
        {
            Documentation = ExtractDocumentation(tsInterface.Documentation),
            Name = tsInterface.Name,
        };
        translation.Interface = csInterface;

        // TODO: Generics
        if (tsInterface.GenericParams.Length > 0) throw new NotImplementedException();

        // TODO: Bases
        if (tsInterface.Bases.Length > 0) throw new NotImplementedException();

        // Fields
        foreach (var field in tsInterface.Fields)
        {
            var prop = this.TranslateField(field, null);
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
        translation.Other = csEnum;

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

    private Cs.Property TranslateField(Ts.Field field, IList<Cs.Declaration>? additionalDecls)
    {
        if (field is Ts.SimpleField simpleField)
        {
            var propType = this.TranslateType(simpleField.Type, additionalDecls);
            return new Cs.Property(
                Documentation: ExtractDocumentation(simpleField.Documentation),
                Type: propType,
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
        bool needsInterface = false)
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
            var decl = translation?.Class
                    ?? translation?.Other;
            return decl is null
                ? throw new InvalidOperationException()
                : new Cs.DeclarationType(decl);
        }
        else if (type is Ts.UnionTypeExpression union)
        {
            var elements = union.Alternatives
                .Select(alt => this.TranslateType(alt, additionalDecls))
                .ToImmutableArray();
            return new Cs.DiscriminatedUnionType(elements);
        }
        else if (type is Ts.ArrayTypeExpression array)
        {
            var element = this.TranslateType(array.ElementType, additionalDecls);
            return new Cs.ArrayType(element);
        }
        else if (type is Ts.AnonymousTypeExpression anon)
        {
            // TODO
            throw new NotImplementedException();
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

    private static string Capitalize(string s) => $"{char.ToUpper(s[0])}{s[1..]}";

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
