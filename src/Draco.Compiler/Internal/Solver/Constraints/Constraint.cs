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

    public override string ToString() => this.ToString(format: false);

    public string ToString(bool format)
    {
        static bool IsSolverTaskCompletionSource(Type t) =>
            t.IsGenericType && t.GetGenericTypeDefinition() == typeof(SolverTaskCompletionSource<>);

        var relevantProps = this
            .GetType()
            .GetProperties()
            .Where(p => p.Name != nameof(this.Locator)
                     && !IsSolverTaskCompletionSource(p.PropertyType))
            .ToList();

        var result = new StringBuilder();
        var indent = 0;

        void AppendIndentation()
        {
            if (!format) return;
            result!.Append(' ', indent * 2);
        }

        void AppendNewline()
        {
            if (!format) return;
            result!.AppendLine();
        }

        void AppendValue(object? arg)
        {
            switch (arg)
            {
            case null:
                result.Append("null");
                break;
            case string s:
                result.Append(s);
                break;
            case Argument a:
                result.Append(a.Type);
                break;
            case IEnumerable e:
            {
                if (e.Cast<object?>().Count() <= 1)
                {
                    // Keep in one line
                    result.Append('[');
                    foreach (var item in e) AppendValue(item);
                    result.Append(']');
                }
                else
                {
                    // Break into multiple lines
                    result.Append('[');
                    AppendNewline();
                    ++indent;
                    foreach (var item in e)
                    {
                        AppendIndentation();
                        AppendValue(item);
                        result.Append(',');
                        AppendNewline();
                    }
                    --indent;
                    AppendIndentation();
                    result.Append(']');
                }
                break;
            }
            default:
                result.Append(arg.ToString() ?? "<?>");
                break;
            }
        }

        result.Append(this.GetType().Name);
        result.Append('(');

        AppendNewline();
        ++indent;

        foreach (var p in relevantProps)
        {
            AppendIndentation();
            result.Append(p.Name).Append(": ");

            var pValue = p.GetValue(this);
            AppendValue(pValue);

            result.Append(", ");
            AppendNewline();
        }

        --indent;
        AppendIndentation();
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
