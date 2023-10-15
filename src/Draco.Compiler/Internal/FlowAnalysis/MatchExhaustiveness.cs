using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Checks, if match expressions are exhaustive.
/// </summary>
internal sealed class MatchExhaustiveness : BoundTreeVisitor
{
    public static void Analyze(SourceGlobalSymbol global, IntrinsicSymbols intrinsicSymbols, DiagnosticBag diagnostics)
    {
        var pass = new MatchExhaustiveness(intrinsicSymbols, diagnostics);
        global.Value?.Accept(pass);
    }

    public static void Analyze(SourceFunctionSymbol function, IntrinsicSymbols intrinsicSymbols, DiagnosticBag diagnostics)
    {
        var pass = new MatchExhaustiveness(intrinsicSymbols, diagnostics);
        function.Body.Accept(pass);
    }

    private readonly IntrinsicSymbols intrinsicSymbols;
    private readonly DiagnosticBag diagnostics;

    public MatchExhaustiveness(IntrinsicSymbols intrinsicSymbols, DiagnosticBag diagnostics)
    {
        this.intrinsicSymbols = intrinsicSymbols;
        this.diagnostics = diagnostics;
    }

    public override void VisitMatchExpression(BoundMatchExpression node)
    {
        base.VisitMatchExpression(node);

        // We build up the relevant arms
        var arms = node.MatchArms
            .Select(a => DecisionTree.Arm(a.Pattern, a.Guard, a))
            .ToImmutableArray();
        // From that we build the decision tree
        var decisionTree = DecisionTree.Build(this.intrinsicSymbols, node.MatchedValue, arms);

        if (!decisionTree.IsExhaustive)
        {
            // Report
            var diagBuilder = Diagnostic.CreateBuilder()
                .WithTemplate(FlowAnalysisErrors.NonExhaustiveMatchExpression)
                .WithLocation(node.Syntax?.Location);
            var example = decisionTree.UnhandledExample;
            if (example is not null)
            {
                diagBuilder.WithRelatedInformation(DiagnosticRelatedInformation.Create(
                    location: node.Syntax?.Location,
                    format: "for example, the pattern {0} is not handled",
                    formatArgs: DecisionTree.ToDisplayString(example)));
            }
            this.diagnostics.Add(diagBuilder.Build());
        }

        foreach (var (covers, redundant) in decisionTree.Redundancies)
        {
            // Report
            this.diagnostics.Add(Diagnostic.Create(
                template: FlowAnalysisErrors.MatchPatternAlreadyHandled,
                location: redundant.Syntax?.Location,
                relatedInformation: ImmutableArray.Create(DiagnosticRelatedInformation.Create(
                    location: covers.Syntax?.Location,
                    "the case covering this one can be found here"))));
        }
    }
}