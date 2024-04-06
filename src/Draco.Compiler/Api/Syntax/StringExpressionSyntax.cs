using System;
using System.Linq;

namespace Draco.Compiler.Api.Syntax;
public partial class StringExpressionSyntax
{
    public int Padding => this.CloseQuotes.LeadingTrivia.Aggregate(0, (value, right) =>
        {
            if (right.Kind == TriviaKind.Newline) return 0;
            return value + right.Span.Length;
        });
}
