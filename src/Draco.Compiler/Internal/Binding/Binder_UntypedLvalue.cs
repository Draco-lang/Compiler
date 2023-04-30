using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given syntax node to an untyped lvalue.
    /// </summary>
    /// <param name="syntax">The lvalue to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The untyped lvalue for <paramref name="syntax"/>.</returns>
    protected virtual UntypedLvalue BindLvalue(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedExpressionSyntax => new UntypedUnexpectedLvalue(syntax),
        GroupingExpressionSyntax group => this.BindLvalue(group.Expression, constraints, diagnostics),
        NameExpressionSyntax name => this.BindNameLvalue(name, constraints, diagnostics),
        _ => this.BindIllegalLvalue(syntax, constraints, diagnostics),
    };

    private UntypedLvalue BindNameLvalue(NameExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
        switch (symbol)
        {
        case UntypedLocalSymbol local:
            return new UntypedLocalLvalue(syntax, local, constraints.GetLocalType(local));
        case GlobalSymbol global:
            return new UntypedGlobalLvalue(syntax, global);
        default:
        {
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.IllegalLvalue,
                location: syntax?.Location));
            return new UntypedIllegalLvalue(syntax);
        }
        }
    }

    private UntypedLvalue BindIllegalLvalue(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // TODO: Should illegal lvalues contain an expression we still bind?
        // It could result in more errors within the expression, which might be useful
        diagnostics.Add(Diagnostic.Create(
            template: SymbolResolutionErrors.IllegalLvalue,
            location: syntax?.Location));
        return new UntypedIllegalLvalue(syntax);
    }
}
