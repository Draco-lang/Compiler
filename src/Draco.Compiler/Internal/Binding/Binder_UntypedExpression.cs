using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given syntax node to an untyped expression.
    /// </summary>
    /// <param name="syntax">The syntax to bind.</param>
    /// <returns>The untyped expression for <paramref name="syntax"/>.</returns>
    protected UntypedExpression BindExpression(SyntaxNode syntax) => syntax switch
    {
        NameExpressionSyntax name => this.BindNameExpression(name),
        IfExpressionSyntax @if => this.BindIfExpression(@if),
        RelationalExpressionSyntax rel => this.BindRelationalExpression(rel),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedExpression BindNameExpression(NameExpressionSyntax syntax)
    {
        var lookup = this.LookupValueSymbol(syntax.Name.Text, syntax);
        if (!lookup.FoundAny)
        {
            // TODO
            throw new NotImplementedException();
        }
        if (lookup.Symbols.Count > 1)
        {
            // TODO: Multiple symbols, potental overloading
            throw new NotImplementedException();
        }
        return lookup.Symbols[0] switch
        {
            ParameterSymbol param => new UntypedParameterExpression(syntax, param),
            _ => throw new InvalidOperationException(),
        };
    }

    private UntypedExpression BindIfExpression(IfExpressionSyntax syntax)
    {
        var condition = this.BindExpression(syntax.Condition);
        var then = this.BindExpression(syntax.Then);
        var @else = syntax.Else is null
            ? UntypedTreeFactory.UnitExpression()
            : this.BindExpression(syntax.Else.Expression);
        return new UntypedIfExpression(syntax, condition, then, @else);
    }

    private UntypedExpression BindRelationalExpression(RelationalExpressionSyntax syntax)
    {
        var first = this.BindExpression(syntax.Left);
        var comparisons = ImmutableArray.CreateBuilder<UntypedComparison>();
        foreach (var cmpSyntax in syntax.Comparisons)
        {
            var symbolName = ComparisonOperatorSymbol.GetComparisonOperatorName(cmpSyntax.Operator.Kind);
            var lookup = this.LookupValueSymbol(symbolName, syntax);
            if (!lookup.FoundAny || lookup.Symbols.Count > 1)
            {
                // TODO: Handle overload or illegal
                throw new NotImplementedException();
            }
            var symbol = (ComparisonOperatorSymbol)lookup.Symbols[0];
            var right = this.BindExpression(cmpSyntax.Right);
            comparisons.Add(new UntypedComparison(cmpSyntax, symbol, right));
        }
        return new UntypedRelationalExpression(syntax, first, comparisons.ToImmutable());
    }
}
