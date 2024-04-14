using System;
using System.Collections;
using System.Linq;
using System.Text;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// Base class for all the constraints.
/// </summary>
/// <param name="Locator">The locator of the constraint.</param>
internal abstract record class ConstraintBase(ConstraintLocator? Locator, DiagnosticTemplate? Template = null)
{
    public override string ToString()
    {
        static string ArgumentToString(object? arg) => arg switch
        {
            string s => s,
            IEnumerable e => $"[{string.Join(", ", e.Cast<object>().Select(ArgumentToString))}]",
            null => "null",
            _ => arg.ToString() ?? "null",
        };

        var relevantProps = this
            .GetType()
            .GetProperties()
            .Where(p => p.Name != nameof(this.Locator)
                     && p.Name != nameof(this.Template)
                     && p.Name != nameof(Member.CompletionSource));

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
            .WithTemplate(this.Template)
            .WithLocation(this.Locator);
        config(builder);
        diagnostics.Add(builder.Build());
    }
}
