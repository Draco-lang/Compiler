using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Syntax;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    protected virtual void ConstraintReturnType(
        SyntaxNode returnSyntax,
        BindingTask<BoundExpression> returnValue,
        ConstraintSolver constraints,
        DiagnosticBag diagnostics)
    {
        if (this.ContainingSymbol is not FunctionSymbol containingFunction)
        {
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.IllegalReturn,
                location: returnSyntax.Location));
            return;
        }
        var returnTypeSyntax = (containingFunction as SyntaxFunctionSymbol)?.DeclaringSyntax.ReturnType?.Type;
        constraints.Assignable(
            containingFunction.ReturnType,
            returnValue.GetResultType(returnSyntax, constraints, diagnostics),
            ConstraintLocator.Syntax(returnSyntax)
                .WithRelatedInformation(
                    format: "return type declared to be {0}",
                    formatArgs: containingFunction.ReturnType,
                    location: returnTypeSyntax?.Location));
    }

    internal void CheckVisibility(ConstraintLocator? locator, Symbol symbol, string kind, DiagnosticBag diagnostics) =>
        this.CheckVisibility(locator?.GetReferencedSyntax(), symbol, kind, diagnostics);

    internal void CheckVisibility(SyntaxNode? syntax, Symbol symbol, string kind, DiagnosticBag diagnostics)
    {
        // If the symbol is an error, don't propagate errors
        if (symbol.IsError) return;
        // Overloads are reported at resolution site
        if (symbol is FunctionGroupSymbol) return;
        if (symbol.IsVisibleFrom(this.ContainingSymbol)) return;

        diagnostics.Add(Diagnostic.Create(
            template: SymbolResolutionErrors.InaccessibleSymbol,
            location: syntax?.Location,
            formatArgs: [kind, symbol.Name]));
    }
}
