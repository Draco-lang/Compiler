using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Draco.SourceGeneration.BoundTree;
using static Draco.SourceGeneration.TemplateUtils;

namespace Draco.SourceGeneration.SyntaxTree;

internal static class Template
{
    public static string GenerateTokens(Tree tree) => FormatCSharp($$"""
namespace Draco.Compiler.Api.Syntax;

#nullable enable

/// <summary>
/// The different kinds of tokens in the syntax tree.
/// </summary>
public enum TokenKind
{
    {{ForEach(tree.Tokens, ",", token => $$"""
    /// <summary>
    /// {{token.Documentation}}
    /// </summary>
    {{token.Name}}
    """)}}
}

public static partial class SyntaxFacts
{
    /// <summary>
    /// Attempts to retrieve the textual representation of a <see cref="TokenKind"/>.
    /// </summary>
    /// <param name="tokenKind">The <see cref="TokenKind"/> to get the text of.</param>
    /// <returns>The textual representation of <paramref name="tokenKind"/>, or null, if it doesn't have a
    /// unique representation.</returns>
    public static string? GetTokenText(TokenKind tokenKind) => tokenKind switch
    {
        {{ForEach(tree.Tokens, token => NotNull(token.Text, text => $"TokenKind.{token.Name} => \"{text}\","))}}
        _ => null
    };
}

public static partial class SyntaxFactory
{
    {{TokenFactories(tree)}}
}

#nullable restore
""");

    public static string GenerateGreenTree(Tree tree) => FormatCSharp($$"""
using System.Collections.Generic;

namespace Draco.Compiler.Internal.Syntax;

#nullable enable

{{ForEach(tree.Nodes, node => GreenNodeClass(tree, node))}}

/// <summary>
/// Visitor base class for <see cref="{{tree.Root.Name}}"/>.
/// </summary>
internal abstract partial class SyntaxVisitor
{
    {{Visitors(tree.Nodes, "void", null)}}
}

/// <summary>
/// Visitor base class for <see cref="{{tree.Root.Name}}"/>.
/// </summary>
/// <typeparam name="TResult">
/// The return type of the visitor methods.
/// </typeparam>
internal abstract partial class SyntaxVisitor<TResult>
{
    {{Visitors(tree.Nodes, "TResult", "default!")}}
}

/// <summary>
/// A base class for rewriting <see cref="{{tree.Root.Name}}"/>.
/// </summary>
internal abstract partial class SyntaxRewriter : SyntaxVisitor<{{tree.Root.Name}}>
{
    {{Rewriters(tree)}}
}

#nullable restore
""");

    public static string GenerateRedTree(Tree tree) => FormatCSharp($$"""
using System.Collections.Generic;

namespace Draco.Compiler.Api.Syntax;

#nullable enable

{{ForEach(tree.Nodes, node => RedNodeClass(tree, node))}}

/// <summary>
/// Visitor base class for <see cref="{{tree.Root.Name}}"/>.
/// </summary>
public abstract partial class SyntaxVisitor
{
    {{Visitors(tree.Nodes, "void", null)}}
}

/// <summary>
/// Visitor base class for <see cref="{{tree.Root.Name}}"/>.
/// </summary>
/// <typeparam name="TResult">
/// The return type of the visitor methods.
/// </typeparam>
public abstract partial class SyntaxVisitor<TResult>
{
    {{Visitors(tree.Nodes, "TResult", "default!")}}
}

public static partial class SyntaxFactory
{
    {{SyntaxFactories(tree)}}
}

#nullable restore
""");

    private static string GreenNodeClass(Tree tree, Node node) => $$"""
    /// <summary>
    /// {{node.Documentation}}
    /// </summary>
    internal {{ClassHeader(node)}}
    {
        {{ForEach(node.Fields, field => $"{FieldPrefix(field)} {{ get; }}")}}

        {{Children(tree, node)}}

        {{ProtectedPublic(node)}} {{node.Name}}(
            {{ForEach(node.Fields, ", ", field => When(!field.Abstract, $"{field.Type} {CamelCase(field.Name)}"))}})
        {
            {{ForEach(node.Fields, field => When(!field.Abstract && field.IsToken, $$"""
                if (
                    {{When(field.IsNullable, $"{CamelCase(field.Name)} is not null &&")}}
                    {{CamelCase(field.Name)}}.Kind is not {{ForEach(field.TokenKinds, " and not ", kind => $"Api.Syntax.TokenKind.{kind}")}})
                {
                    throw new System.ArgumentOutOfRangeException(
                        nameof({{CamelCase(field.Name)}}),
                        "the token must be of kind {{ForEach(field.TokenKinds, " or ", k => k)}}");
                }
            """))}}

            {{ForEach(node.Fields, field => When(!field.Abstract, $"this.{field.Name} = {CamelCase(field.Name)};"))}}
        }

        {{When(node.IsAbstract,
            whenTrue: $"""
                public abstract {When(node.Base is not null, "override")} Api.Syntax.{node.Name} ToRedNode(
                    Api.Syntax.SyntaxTree tree, Api.Syntax.{tree.Root.Name}? parent, int fullPosition);
                """,
            whenFalse: $"""
                public override Api.Syntax.{node.Name} ToRedNode(
                    Api.Syntax.SyntaxTree tree, Api.Syntax.{tree.Root.Name}? parent, int fullPosition) =>
                    new Api.Syntax.{node.Name}(tree, parent, fullPosition, this);
                """)}}

        {{When(!node.IsAbstract, $$"""
            /// <summary>
            /// Updates this <see cref="{{node.Name}}"/> with the new provided data.
            /// The node is only reinstantiated, if the passed in data is different.
            /// </summary>
            {{ForEach(node.Fields, "\n", field => NotNull(field.Documentation, doc => $"""
                /// <param name="{CamelCase(field.Name)}">
                /// {field.Documentation}
                /// </param>
            """))}}
            /// <returns>
            /// The constructed <see cref="{{node.Name}}"/>, or this instance, if the passed in data is identical
            /// to the old one.
            /// </returns>
            public {{node.Name}} Update({{ForEach(node.Fields, ", ", field => $"{field.Type} {CamelCase(field.Name)}")}})
            {
                if ({{ForEach(node.Fields, " && ", field => $"Equals(this.{field.Name}, {CamelCase(field.Name)})")}}) return this;
                else return new {{node.Name}}({{ForEach(node.Fields, ", ", field => CamelCase(field.Name))}});
            }

            /// <summary>
            /// Updates this <see cref="{{node.Name}}"/> with the new provided data.
            /// The node is only reinstantiated, if the passed in data is different.
            /// </summary>
            /// <param name="children">The child nodes of this node.</param>
            /// <returns>
            /// The constructed <see cref="{{node.Name}}"/>, or this instance, if the passed in data is identical
            /// to the old one.
            /// </returns>
            public {{node.Name}} Update(IEnumerable<SyntaxNode?> children)
            {
                var enumerator = children.GetEnumerator();
                {{ForEach(node.Fields, field => $$"""
                    if (!enumerator.MoveNext())
                    {
                        throw new System.ArgumentOutOfRangeException(
                            nameof(children),
                            "the sequence contains too few children for this node");
                    }
                    var {{CamelCase(field.Name)}} = ({{field.Type}})enumerator.Current!;
                """)}}
                if (enumerator.MoveNext())
                {
                    throw new System.ArgumentOutOfRangeException(
                        nameof(children),
                        "the sequence contains too many children for this node");
                }
                return this.Update({{ForEach(node.Fields, ", ", field => CamelCase(field.Name))}});
            }
        """)}}

        {{Accept(node)}}
    }
    """;

    private static string RedNodeClass(Tree tree, Node node) => $$"""
    /// <summary>
    /// {{node.Documentation}}
    /// </summary>
    public {{ClassHeader(node)}}
    {
        {{ForEach(node.Fields, field => When(field.Abstract,
            whenTrue: $$"""
                {{FieldPrefix(field)}} { get; }
                """,
            whenFalse: $$"""
                {{FieldPrefix(field)}} =>
                {{When(field.IsNullable,
                    "Internal.InterlockedUtils.InitializeMaybeNull(",
                    "System.Threading.LazyInitializer.EnsureInitialized(")}}
                    ref this.{{CamelCase(field.Name)}},
                    () => ({{field.Type}})this.Green.{{field.Name}}{{Nullable(field)}}
                        .ToRedNode(this.Tree, this, {{AccumulateFullWidth(node, field)}}));
                private {{field.NonNullableType}}? {{CamelCase(field.Name)}};
                """))}}

        {{When(node.IsAbstract,
            whenTrue: $$"""
                internal abstract {{When(node.Base is not null, "override")}} Internal.Syntax.{{node.Name}} Green { get; }
                """,
            whenFalse: $$"""
                internal override Internal.Syntax.{{node.Name}} Green { get; }
                """)}}

        {{Children(tree, node)}}

        {{When(node.IsAbstract,
            whenTrue: When(node.Base is not null, $$"""
                internal {{node.Name}}(SyntaxTree tree, {{tree.Root.Name}}? parent, int fullPosition)
                    : base(tree, parent, fullPosition)
                {
                }
                """),
            whenFalse: $$"""
                internal {{node.Name}}(SyntaxTree tree, {{tree.Root.Name}}? parent, int fullPosition, Internal.Syntax.{{node.Name}} green)
                    : base(tree, parent, fullPosition)
                {
                    this.Green = green;
                }
                """)}}

        {{Accept(node)}}
    }
    """;

    private static string AccumulateFullWidth(Node node, Field current) =>
        $"this.FullPosition {ForEach(node.Fields.TakeWhile(f => f != current), field => $" + {When(field.IsNullable,
            whenTrue: $"(this.Green.{field.Name}?.FullWidth ?? 0)",
            whenFalse: $"this.Green.{field.Name}.FullWidth")}")}";

    private static string TokenFactories(Tree tree) => ForEach(tree.Tokens, token => When(token.Text is not null, $$"""
    public static SyntaxToken {{token.Name}} { get; } = Token(
        TokenKind.{{token.Name}}
        {{NotNull(token.Value, value => $", {value}")}});
    """));

    private static string SyntaxFactories(Tree tree) => ForEach(tree.Nodes, node => When(!node.IsAbstract, $$"""
    /// <summary>
    /// Constructs a new <see cref="{{node.Name}}"/>.
    /// </summary>
    {{ForEach(node.Fields, "\n", field => NotNull(field.Documentation, doc => $"""
         /// <param name="{CamelCase(field.Name)}">
         /// {field.Documentation}
         /// </param>
         """))}}
    /// <returns>
    /// The constructed <see cref="{{node.Name}}"/>.
    /// </returns>
    public static {{node.Name}} {{FactoryName(node)}}(
        {{ForEach(node.Fields, ", ", field => $"{field.Type} {CamelCase(field.Name)}")}}) =>
        new Internal.Syntax.{{node.Name}}(
            {{ForEach(node.Fields, ", ", field => $"({InternalType(field.Type)}){CamelCase(field.Name)}{Nullable(field)}.Green")}}
        ).ToRedNode(null!, null, 0);

    {{SimplifiedSyntaxFactory(tree, node)}}
    """));

    private static string SimplifiedSyntaxFactory(Tree tree, Node node)
    {
        var facadedFields = Simplify(tree, node, out var anySimplified);
        if (!anySimplified) return string.Empty;

        return $$"""
            /// <returns>
            /// The constructed <see cref="{{node.Name}}"/>.
            /// </returns>
            {{ForEach(facadedFields, "\n", field => When(field.IsParameter, $"""
                /// <param name="{field.ParameterName}">
                /// {field.Documentation}
                /// </param>
                """))}}
            /// <returns>
            /// The constructed <see cref="{{node.Name}}"/>.
            /// </returns>
            public static {{node.Name}} {{FactoryName(node)}}(
                {{ForEach(facadedFields.Where(f => f.IsParameter), ", ", field =>
                    $"{field.Type} {field.ParameterName} {NotNull(field.DefaultValue, v => $"= {v}")}")}}) =>
                {{FactoryName(node)}}({{ForEach(facadedFields, ", ", field => field.ReferenceValue)}});
            """;
    }

    private static string Children(Tree tree, Node node) => When(!node.IsAbstract, $$"""
    public override IEnumerable<{{tree.Root.Name}}> Children
    {
        get
        {
            {{ForEach(node.Fields, field => $"""
                {When(field.IsNullable, $"if (this.{field.Name} is not null)")}
                yield return this.{field.Name};
            """)}}
            yield break;
        }
    }
    
    """);

    private static string Accept(Node node) => When(node.IsAbstract && node.Base is null,
        whenTrue: """
            public abstract void Accept(SyntaxVisitor visitor);
            public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);
            """,
        whenFalse: $"""
            public override void Accept(SyntaxVisitor visitor) =>
                visitor.{VisitorName(node)}(this);
            public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) =>
                visitor.{VisitorName(node)}(this);
            """);

    private static string Visitors(IEnumerable<Node> nodes, string returnType, string? returnValue) => ForEach(nodes, node => $$"""
    {{When(node.IsAbstract,
        whenTrue: $$"""
            public virtual {{returnType}} {{VisitorName(node)}}({{node.Name}} node)
            {
                {{When(returnValue is not null, "return ")}} node.Accept(this);
            }
            """,
        whenFalse: $$"""
            public virtual {{returnType}} {{VisitorName(node)}}({{node.Name}} node)
            {
                {{ForEach(node.Fields, field => $"node.{field.Name}{Nullable(field)}.Accept(this);")}}
                {{NotNull(returnValue, v => $"return {v};")}}
            }
            """)}}
    """);

    private static string Rewriters(Tree tree) => ForEach(tree.Nodes, node => When(!node.IsAbstract, $$"""
    public override {{tree.Root.Name}} {{VisitorName(node)}}({{node.Name}} node)
    {
        {{ForEach(node.Fields, field =>
            $"var {CamelCase(field.Name)} = ({field.Type})node.{field.Name}{Nullable(field)}.Accept(this);")}}
        return node.Update({{ForEach(node.Fields, ", ", field => CamelCase(field.Name))}});
    }
    """));

    private static string VisitorName(Node node) => $"Visit{RemoveSuffix(node.Name, "Syntax")}";

    private static string ClassHeader(Node node) =>
        $"{AbstractSealed(node)} partial class {node.Name} {Base(node)}";

    private static string FieldPrefix(Field field) => $$"""
        {{NotNull(field.Documentation, doc => $"""
        /// <summary>
        /// {doc}
        /// </summary>
        """)}}
        public {{When(field.Abstract, "abstract")}} {{When(field.Override, "override")}} {{field.Type}} {{field.Name}}
        """;

    private static string InternalType(string type) =>
        $"Internal.Syntax.{type.Replace("<", "<Internal.Syntax.")}";

    private static string FactoryName(Node node) => RemoveSuffix(node.Name, "Syntax");
    private static string AbstractSealed(Node node) => When(node.IsAbstract, "abstract", "sealed");
    private static string ProtectedPublic(Node node) => When(node.IsAbstract, "protected", "public");
    private static string Base(Node node) => NotNull(node.Base, b => $": {b.Name}");
    private static string Nullable(Field field) => When(field.IsNullable, "?");

    private readonly record struct FieldFacade(
        string? Documentation,
        string? ParameterName,
        string? Type,
        string ReferenceValue,
        string? DefaultValue = null)
    {
        public bool IsParameter => this.ParameterName is not null;
    }

    private static IEnumerable<FieldFacade> Simplify(Tree tree, Node node, out bool anySimplified)
    {
        anySimplified = false;
        var result = new List<FieldFacade>();

        // We go backwards to determine the default values
        for (var i = node.Fields.Count - 1; i >= 0; --i)
        {
            var field = node.Fields[i];

            var facade = Simplify(tree, field, ref anySimplified);
            // We can make it null-defaulted, if this field is nullable and any fields THAT ARE PARAMETERS ahead were nullable
            var canBeNulled = field.IsNullable
                           && result.All(f => !f.IsParameter || f.DefaultValue is not null);
            if (canBeNulled)
            {
                facade = facade with { DefaultValue = "null" };
                anySimplified = true;
            }

            result.Insert(0, facade);
        }
        return result;
    }

    private static FieldFacade Simplify(Tree tree, Field field, ref bool anySimplified)
    {
        if (!field.IsNullable
         && field.IsToken
         && field.TokenKinds.Count == 1
         && tree.GetTokenFromKind(field.TokenKinds[0]) is { Text: not null } tokenKind)
        {
            // A trivially substitutable token, as it's not nullable (required) and there is a single token that can be substituted
            // with a well-known text
            anySimplified = true;
            return new FieldFacade(
                Documentation: null,
                ParameterName: null,
                Type: null,
                ReferenceValue: $"Token(TokenKind.{tokenKind.Name} {NotNull(tokenKind.Value, v => $", {v}")})");
        }

        if (field.IsToken && field.TokenKinds.Count == 1)
        {
            var kind = field.TokenKinds[0];
            if (kind == "Identifier" && !field.IsNullable) // We assume identifiers are always required
            {
                // We can simplify the identifier token to a string
                anySimplified = true;
                return new FieldFacade(
                    Documentation: field.Documentation,
                    ParameterName: CamelCase(field.Name),
                    Type: "string",
                    ReferenceValue: $"Identifier({CamelCase(field.Name)})");
            }
        }

        // Regular field
        return new FieldFacade(
            Documentation: field.Documentation,
            ParameterName: CamelCase(field.Name),
            Type: field.Type,
            ReferenceValue: CamelCase(field.Name));
    }
}
