using System.Collections.Generic;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;
using Draco.Chr.Tracing;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;
using IChrConstraint = Draco.Chr.Constraints.IConstraint;
using Constraint = Draco.Compiler.Internal.Solver.Constraints.Constraint;
using System.Text;
using System.IO;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A tracer that visualizes the constraint graph.
/// </summary>
internal sealed class ConstraintGraphTracer : ITracer
{
    private readonly DotGraphBuilder<object> graphBuilder = new(isDirected: true);
    private readonly HashSet<SyntaxTree> syntaxTrees = [];
    private int stepIndex;

    public ConstraintGraphTracer()
    {
        this.graphBuilder
            .WithName("constraints")
            .WithRankDir(DotAttribs.RankDir.LeftToRight);
    }

    /// <summary>
    /// Retrieves the DOT graph.
    /// </summary>
    /// <returns>The DOT graph code.</returns>
    public string GetDotGraph() => this.graphBuilder.ToDot();

    public void Start(ConstraintStore store)
    {
        // Connect each constraint to its source
        foreach (var constraint in store)
        {
            var constraintValue = this.GetConstraint(constraint);
            var location = this.GetLocation(constraintValue);
            if (location is null) continue;

            var (vertexKey, lineNumber) = location.Value;
            this.graphBuilder.AddEdge(vertexKey, constraintValue, fromPort: $"{lineNumber}:e");
        }
    }

    public void End(ConstraintStore store) { }

    public void BeforeMatch(
        Rule rule,
        IEnumerable<IChrConstraint> constraints,
        ConstraintStore store)
    {
        ++this.stepIndex;
        // We want to index the rule with the step index to have it instantiated each application
        var indexedRule = (rule, this.stepIndex);
        // Decorate the rule vertex with its name
        this.graphBuilder
            .AddVertex(indexedRule)
            .WithLabel(rule.Name)
            .WithXLabel(this.stepIndex.ToString());
        // Connect each constraint to the rule
        foreach (var constraint in constraints)
        {
            var constraintValue = this.GetConstraint(constraint);
            this.graphBuilder.AddEdge(constraintValue, indexedRule);
        }
    }

    public void AfterMatch(
        Rule rule,
        IEnumerable<IChrConstraint> matchedConstraints,
        IEnumerable<IChrConstraint> newConstraints,
        ConstraintStore store)
    {
        // We want to index the rule with the step index to have it instantiated each application
        var indexedRule = (rule, this.stepIndex);
        // Connect the rule to the new constraints
        foreach (var constraint in newConstraints)
        {
            var constraintValue = this.GetConstraint(constraint);
            this.graphBuilder.AddEdge(indexedRule, constraintValue);
        }
    }

    public void Flush() { }

    private Constraint GetConstraint(IChrConstraint chrConstraint)
    {
        var constraint = (Constraint)chrConstraint.Value;
        this.graphBuilder
            .AddVertex(constraint)
            .WithShape(DotAttribs.Shape.Rectangle)
            .WithLabel(constraint.ToString());
        return constraint;
    }

    private (SyntaxTree VertexKey, int LineNumber)? GetLocation(Constraint constraint)
    {
        var syntax = constraint.Locator?.GetReferencedSyntax();
        if (syntax is null) return null;

        var location = syntax.Location;
        if (location.IsNone) return null;
        if (location.Range is null) return null;

        var syntaxTree = syntax.Tree;
        if (this.syntaxTrees.Add(syntaxTree))
        {
            // We need to generate the line-number -> source-text HTML table
            var htmlCode = new StringBuilder();
            htmlCode.AppendLine("""
                <table border="0" cellborder="1" cellspacing="0">
                    <tr>
                        <td><i>Line</i></td>
                        <td>Code</td>
                    </tr>
                """);

            // NOTE: We use ToString to avoid SourceText issues with syntax trees built on the fly
            var lines = TextToLines(syntaxTree.ToString());

            // Generate each row
            for (var i = 0; i < lines.Count; i++)
            {
                htmlCode.AppendLine($"""
                    <tr>
                        <td>{i + 1}</td>
                        <td port="{i}">{lines[i]}</td>
                    </tr>
                    """);
            }

            htmlCode.AppendLine("</table>");

            // Add it as an attribute
            this.graphBuilder
                .AddVertex(syntaxTree)
                .WithShape(DotAttribs.Shape.None)
                .WithHtmlAttribute("label", htmlCode.ToString());
        }

        return (syntaxTree, location.Range.Value.Start.Line);
    }

    private static List<string> TextToLines(string text)
    {
        using var reader = new StringReader(text);
        var lines = new List<string>();
        while (true)
        {
            var line = reader.ReadLine();
            if (line is null) break;
            lines.Add(line);
        }
        return lines;
    }
}
