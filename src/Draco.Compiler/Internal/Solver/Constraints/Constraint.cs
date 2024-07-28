using System;
using System.Collections;
using System.Linq;
using System.Text;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver.OverloadResolution;
using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// Base class for a CHR type-system constraint.
/// </summary>
/// <param name="locator">The locator for the constraint.</param>
/// <param name="diagnosticTemplate">The diagnostic template for the constraint to simplify error reporting.</param>
internal abstract class Constraint(ConstraintLocator? locator, DiagnosticTemplate? diagnosticTemplate = null)
{
    /// <summary>
    /// The constraint locator.
    /// </summary>
    public ConstraintLocator? Locator { get; } = locator;

    public override string ToString()
    {
        static string ArgumentToString(object? arg) => arg switch
        {
            string s => s,
            Argument a => a.Type.ToString(),
            IEnumerable e => $"[{string.Join(", ", e.Cast<object>().Select(ArgumentToString))}]",
            null => "null",
            _ => arg.ToString() ?? "null",
        };

        static bool IsSolverTaskCompletionSource(Type t) =>
            t.IsGenericType && t.GetGenericTypeDefinition() == typeof(SolverTaskCompletionSource<>);

        var relevantProps = this
            .GetType()
            .GetProperties()
            .Where(p => p.Name != nameof(this.Locator)
                     && !IsSolverTaskCompletionSource(p.PropertyType));

        var result = new StringBuilder();
        result.Append(this.GetType().Name);
        result.Append('(');
        result.AppendJoin(", ", relevantProps.Select(p => $"{p.Name}: {ArgumentToString(p.GetValue(this))}"));
        result.Append(')');
        return result.ToString();
    }

    public void ReportDiagnostic(DiagnosticBag diagnostics, Action<Diagnostic.Builder> config)
    {
        var builder = Diagnostic
            .CreateBuilder()
            .WithTemplate(diagnosticTemplate)
            .WithLocation(this.Locator);
        config(builder);
        diagnostics.Add(builder.Build());
    }
}
