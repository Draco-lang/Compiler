using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Checks, if all read-only variables are assigned exactly once.
/// </summary>
internal sealed class ValAssignment : BoundTreeVisitor
{
    public static void Analyze(SourceGlobalSymbol global, DiagnosticBag diagnostics)
    {
        if (global.Value is null && !global.IsMutable)
        {
            // Not initialized
            diagnostics.Add(Diagnostic.Create(
                template: FlowAnalysisErrors.ImmutableVariableMustBeInitialized,
                location: global.DeclaringSyntax.Location,
                formatArgs: global.Name));
        }

        // Check body
        var pass = new ValAssignment(diagnostics);
        global.Value?.Accept(pass);
    }

    public static void Analyze(SourceFunctionSymbol function, DiagnosticBag diagnostics)
    {
        var pass = new ValAssignment(diagnostics);
        function.Body.Accept(pass);
    }

    private readonly DiagnosticBag diagnostics;

    public ValAssignment(DiagnosticBag diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    public override void VisitLocalDeclaration(BoundLocalDeclaration node)
    {
        if (node.Value is not null || node.Local.IsMutable) return;

        // Immutable not assigned
        this.diagnostics.Add(Diagnostic.Create(
            template: FlowAnalysisErrors.ImmutableVariableMustBeInitialized,
            location: node.Syntax?.Location,
            formatArgs: node.Local.Name));
    }

    public override void VisitAssignmentExpression(BoundAssignmentExpression node)
    {
        base.VisitAssignmentExpression(node);

        var lvalue = node.Left switch
        {
            BoundLocalLvalue l => l.Local as VariableSymbol,
            BoundGlobalLvalue l => l.Global,
            _ => null,
        };

        if (lvalue is null || lvalue.IsMutable) return;

        // Immutable modified
        this.diagnostics.Add(Diagnostic.Create(
            template: FlowAnalysisErrors.ImmutableVariableCanNotBeAssignedTo,
            location: node.Syntax?.Location,
            formatArgs: lvalue.Name));
    }
}
