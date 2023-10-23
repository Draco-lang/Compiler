using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// A formatter for the syntax tree.
/// </summary>
internal sealed class Formatter : SyntaxRewriter
{
    private static readonly object Space = new();
    private static readonly object Newline = new();
    private static readonly object Newline2 = new();

    /// <summary>
    /// The settings of the formatter.
    /// </summary>
    public FormatterSettings Settings { get; }

    private int indentation;
    private SyntaxToken? lastToken;

    public Formatter(FormatterSettings settings)
    {
        this.Settings = settings;
    }

    private IEnumerable<SyntaxNode?> AppendSequence(params object[] elements)
    {
        foreach (var element in elements)
        {
            if (element is null)
            {
                yield return null;
            }
            if (ReferenceEquals(element, Space))
            {
                // TODO: Ensure space between now and lastToken
            }
            else if (ReferenceEquals(element, Newline))
            {
                // TODO: Ensure newline between now and lastToken
            }
            else if (ReferenceEquals(element, Newline2))
            {
                // TODO: Ensure 2 newlines between now and lastToken
            }
            else if (element is SyntaxToken token)
            {
                // TODO: Construct token with accumulated leading trivia
            }
            else if (element is SyntaxNode node)
            {
                yield return node.Accept(this);
            }
            else
            {
                throw new ArgumentException($"can not handle sequence element {element}");
            }
        }
    }
}
