using System.Collections;
using System.Collections.Generic;
using static Draco.SourceGeneration.TemplateUtils;

namespace Draco.SourceGeneration.BoundTree;

internal static class Template
{
    public static string Generate(Tree tree) => FormatCSharp($$"""
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.BoundTree;

#pragma warning disable CS0162
#nullable enable

{{ForEach(tree.Nodes, node => $"""
    {GenerateClass(node)}
    {GenerateFactory(node)}
""")}}

/// <summary>
/// Visitor base class for <see cref="{{tree.Root.Name}}"/>.
/// </summary>
internal abstract partial class BoundTreeVisitor
{
    {{VisitorFunctions(tree, "void", null)}}
}

/// <summary>
/// Visitor base class for <see cref="{{tree.Root.Name}}"/>.
/// </summary>
/// <typeparam name="TResult">The return type of the visitor methods.</typeparam>
internal abstract partial class BoundTreeVisitor<TResult>
{
    {{VisitorFunctions(tree, "TResult", "default!")}}
}

/// <summary>
/// A base class for rewriting <see cref="{{tree.Root.Name}}"/>.
/// </summary>
internal abstract partial class BoundTreeRewriter : BoundTreeVisitor<{{tree.Root.Name}}>
{
    {{RewriterFunctions(tree)}}
}

#nullable restore
#pragma warning restore CS0162
""");

    private static string GenerateClass(Node node) => $$"""
    internal {{ClassHeader(node)}}
    {
        {{ForEach(node.Fields, field => Field(field))}}

        {{ProtectedPublic(node)}} {{node.Name}}(
            Api.Syntax.SyntaxNode? syntax
            {{ForEach(node.Fields, field => $", {field.Type} {CamelCase(field.Name)}")}})
            : base(syntax)
        {
            {{ForEach(node.Fields, field => $"this.{field.Name} = {CamelCase(field.Name)};")}}
        }

        {{When(!node.IsAbstract, $$"""
            public override string ToString()
            {
                var result = new StringBuilder();
                result.Append("{{node.Name}}");
                result.Append('(');

                {{ForEach(node.Fields, field => $$"""
                    {{When(node.Fields.Count > 1, $"result.Append(\"{field.Name}: \");")}}
                    {{When(field.IsArray,
                        whenTrue: $"""
                        result.Append('[');
                        result.AppendJoin(", ", this.{field.Name});
                        result.Append(']');
                        """,
                        whenFalse: $"result.Append(this.{field.Name});")}}
                """)}}

                result.Append(')');
                return result.ToString();
            }
        """)}}

        {{When(!node.IsAbstract, $$"""
            public {{node.Name}} Update(
                {{ForEach(node.Fields, ", ", field => $"{field.Type} {CamelCase(field.Name)}")}})
            {
                {{When(node.Fields.Count > 0,
                    whenTrue: $$"""
                    if ({{ForEach(node.Fields, "&&", field => $"Equals(this.{field.Name}, {CamelCase(field.Name)})")}}) return this;
                    else return new {{node.Name}}(
                        this.Syntax,
                        {{ForEach(node.Fields, ", ", field => CamelCase(field.Name))}});
                    """,
                    whenFalse: "return this;")}}
            }
        """)}}

        {{When(node.IsAbstract && node.Base is null,
            whenTrue: """
                public abstract void Accept(BoundTreeVisitor visitor);
                public abstract TResult Accept<TResult>(BoundTreeVisitor<TResult> visitor);
            """,
            whenFalse: $"""
                public override void Accept(BoundTreeVisitor visitor) =>
                    visitor.{VisitorName(node)}(this);
                public override TResult Accept<TResult>(BoundTreeVisitor<TResult> visitor) =>
                    visitor.{VisitorName(node)}(this);
            """)}}
    }
    """;

    private static string GenerateFactory(Node node) => When(!node.IsAbstract, $$"""
    internal static partial class BoundTreeFactory
    {
        public static {{node.Name}} {{FactoryName(node)}}(
            Api.Syntax.SyntaxNode? syntax
            {{ForEach(node.Fields, field => $", {field.Type} {CamelCase(field.Name)}")}}) => new {{node.Name}}(
            syntax
            {{ForEach(node.Fields, field => $", {CamelCase(field.Name)}")}});
    
        public static {{node.Name}} {{FactoryName(node)}}(
            {{ForEach(node.Fields, ", ", field => $"{field.Type} {CamelCase(field.Name)}")}}) => {{FactoryName(node)}}(
            null
            {{ForEach(node.Fields, field => $", {CamelCase(field.Name)}")}});
    }
    """);

    private static string VisitorFunctions(Tree tree, string returnType, string? returnValue) => ForEach(tree.Nodes, node => When(node.IsAbstract,
    whenTrue: $$"""
    public {{returnType}} {{VisitorName(node)}}({{node.Name}} node)
    {
        {{When(returnValue is null,
            whenTrue: "node.Accept(this);",
            whenFalse: "return node.Accept(this);")}}
    }
    """,
    whenFalse: $$"""
    public virtual {{returnType}} {{VisitorName(node)}}({{node.Name}} node)
    {
        {{ForEach(node.Fields, field => When(tree.HasNodeWithName(field.NonNullableType),
            whenTrue: $"node.{field.Name}{Nullable(field)}.Accept(this);",
            whenFalse: When(
                field.IsArray && tree.HasNodeWithName(field.ElementType),
                $"foreach (var element in node.{field.Name}) element.Accept(this);")))}}
        {{NotNull(returnValue, v => $"return {v};")}}
    }
    """));

    private static string RewriterFunctions(Tree tree) => ForEach(tree.Nodes, node => When(!node.IsAbstract, $$"""
    public override {{tree.Root.Name}} {{VisitorName(node)}}({{node.Name}} node)
    {
        {{ForEach(node.Fields, field => When(tree.HasNodeWithName(field.NonNullableType),
            whenTrue: $"var {CamelCase(field.Name)} = ({field.Type})node.{field.Name}{Nullable(field)}.Accept(this);",
            whenFalse: When(field.IsArray && tree.HasNodeWithName(field.ElementType),
                whenTrue: $"var {CamelCase(field.Name)} = this.VisitArray(node.{field.Name});",
                whenFalse: $"var {CamelCase(field.Name)} = node.{field.Name};")))}}
        return node.Update({{ForEach(node.Fields, ", ", field => CamelCase(field.Name))}});
    }
    """));

    private static string ClassHeader(Node node) =>
        $"{AbstractSealed(node)} partial class {node.Name} {Base(node)}";

    private static string AbstractSealed(Node node) => node.IsAbstract ? "abstract" : "sealed";
    private static string ProtectedPublic(Node node) => node.IsAbstract ? "protected" : "public";
    private static string Base(Node node) => NotNull(node.Base, b => $": {b.Name}");
    private static string Nullable(Field field) => When(field.IsNullable, "?");

    private static string Field(Field field) => $$"""
        public {{When(field.Override, "override")}} {{field.Type}} {{field.Name}} { get; }
        """;

    private static string VisitorName(Node node) => $"Visit{RemovePrefix(node.Name, "Bound")}";
    private static string FactoryName(Node node) => RemovePrefix(node.Name, "Bound");
}
