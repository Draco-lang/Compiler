using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given syntax node to an untyped pattern.
    /// </summary>
    /// <param name="syntax">The syntax to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The untyped pattern for <paramref name="syntax"/>.</returns>
    protected virtual UntypedPattern BindPattern(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        DiscardPatternSyntax discard => this.BindDiscardPattern(discard, constraints, diagnostics),
        LiteralPatternSyntax literal => this.BindLiteralPattern(literal, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedPattern BindDiscardPattern(DiscardPatternSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new UntypedDiscardPattern(syntax);

    private UntypedPattern BindLiteralPattern(LiteralPatternSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (!BinderFacts.TryGetLiteralType(syntax.Literal.Value, this.IntrinsicSymbols, out var literalType))
        {
            throw new InvalidOperationException("can not determine literal type");
        }
        return new UntypedLiteralPattern(syntax, syntax.Literal.Value, literalType);
    }
}
