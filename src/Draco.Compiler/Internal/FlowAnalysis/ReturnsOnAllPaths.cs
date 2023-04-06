using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Performs the check to see if a method returns on all of its execution paths.
/// </summary>
internal sealed class ReturnsOnAllPaths : FlowAnalysisPass<ReturnsOnAllPaths.ReturnStatus>
{
    public static void Analyze(SourceFunctionSymbol function, DiagnosticBag diagnostics)
    {
        var pass = new ReturnsOnAllPaths();
        var result = pass.Analyze(function.Body);
        if (result == ReturnStatus.DoesNotReturn)
        {
            diagnostics.Add(Diagnostic.Create(
                template: FlowAnalysisErrors.DoesNotReturn,
                location: function.DeclarationSyntax.Location,
                formatArgs: function.Name));
        }
    }

    public enum ReturnStatus
    {
        DoesNotReturn,
        Returns,
    }

    public override ReturnStatus Top => ReturnStatus.DoesNotReturn;
    public override ReturnStatus Bottom => ReturnStatus.Returns;

    public override ReturnStatus Clone(in ReturnStatus state) => state;

    public override bool Join(ref ReturnStatus target, in ReturnStatus other)
    {
        // If we don't return, it won't change
        if (target == ReturnStatus.DoesNotReturn) return false;
        // Otherwise we return, so if the other returns too, we don't change either
        if (other == ReturnStatus.Returns) return false;
        // There's change, we don't necessarily return anymore
        target = ReturnStatus.DoesNotReturn;
        return true;
    }

    public override bool Meet(ref ReturnStatus target, in ReturnStatus other)
    {
        // If we already return, there won't be change
        if (target == ReturnStatus.Returns) return false;
        // Otherwise we don't return, so if the other does not return, we don't change either
        if (other == ReturnStatus.DoesNotReturn) return false;
        // There's change, we are returning
        target = ReturnStatus.Returns;
        return true;
    }
}
