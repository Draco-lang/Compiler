using System;
using System.Text;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Api.Syntax.Quoting;

/// <summary>
/// Outputs quoted code as C# code.
/// </summary>
/// <param name="builder">The string builder for the template to append text to.</param>
/// <param name="prettyPrint">Whether to append whitespace.</param>
/// <param name="staticImport">Whether to append <c>SyntaxFactory.</c> before properties and function calls.</param>
internal sealed class CSharpQuoterTemplate(StringBuilder builder, bool prettyPrint, bool staticImport)
{
    private int indentLevel = 0;

    /// <summary>
    /// Generates C# code from a <see cref="QuoteExpression"/>.
    /// </summary>
    /// <param name="expr">The expression to generate code from.</param>
    /// <param name="prettyPrint">Whether to append whitespace.</param>
    /// <param name="staticImport">Whether to append <c>SyntaxFactory.</c> before properties and function calls.</param>
    /// <returns></returns>
    public static string Generate(QuoteExpression expr, bool prettyPrint, bool staticImport)
    {
        var builder = new StringBuilder();
        var template = new CSharpQuoterTemplate(builder, prettyPrint, staticImport);
        template.AppendExpr(expr);
        return builder.ToString();
    }

    private void AppendExpr(QuoteExpression expr)
    {
        switch (expr)
        {
        case QuoteFunctionCall call:
            this.AppendFunctionCall(call);
            break;

        case QuoteProperty(var property):
            this.TryAppendSyntaxFactory();
            builder.Append(property);
            break;

        case QuoteList list:
            this.AppendList(list);
            break;

        case QuoteNull:
            builder.Append("null");
            break;

        case QuoteTokenKind(var kind):
            builder.Append(kind.ToString());
            break;

        case QuoteInteger(var value):
            builder.Append(value);
            break;

        case QuoteFloat(var value):
            builder.Append(value);
            break;

        case QuoteBoolean(var value):
            builder.Append(value ? "true" : "false");
            break;

        case QuoteCharacter(var value):
            builder.Append('\'');
            // Need this for escape characters.
            builder.Append(StringUtils.Unescape(value.ToString()));
            builder.Append('\'');
            break;

        case QuoteString(var value):
            builder.Append('"');
            builder.Append(StringUtils.Unescape(value));
            builder.Append('"');
            break;

        default:
            throw new ArgumentOutOfRangeException(nameof(expr));
        }
    }

    private void AppendFunctionCall(QuoteFunctionCall call)
    {
        var (function, typeArgs, args) = call;

        this.TryAppendSyntaxFactory();
        builder.Append(function);

        if (typeArgs is not [])
        {
            builder.Append('<');
            for (var i = 0; i < typeArgs.Length; i++)
            {
                builder.Append(typeArgs[i]);

                if (i < typeArgs.Length - 1) builder.Append(", ");
            }
            builder.Append('>');
        }

        builder.Append('(');
        this.indentLevel += 1;
        for (var i = 0; i < args.Length; i++)
        {
            this.TryAppendNewLine();
            this.TryAppendIndentation();

            this.AppendExpr(args[i]);

            if (i < args.Length - 1) builder.Append(prettyPrint ? "," : ", ");
        }
        this.indentLevel -= 1;
        builder.Append(')');
    }

    private void AppendList(QuoteList list)
    {
        var values = list.Values;

        builder.Append('[');
        this.indentLevel += 1;
        for (var i = 0; i < values.Length; i++)
        {
            this.TryAppendNewLine();
            this.TryAppendIndentation();

            this.AppendExpr(values[i]);

            if (i < values.Length - 1) builder.Append(prettyPrint ? "," : ", ");
        }
        this.indentLevel -= 1;
        builder.Append(']');
    }

    private void TryAppendSyntaxFactory()
    {
        if (!staticImport) builder.Append("SyntaxFactory.");
    }

    private void TryAppendIndentation()
    {
        // Todo: perhaps parameterize indentation size
        if (prettyPrint) builder.Append(' ', this.indentLevel * 2);
    }

    private void TryAppendNewLine()
    {
        if (prettyPrint) builder.Append(Environment.NewLine);
    }
}
