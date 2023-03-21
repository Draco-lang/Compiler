using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a constraint that the solver uses.
/// </summary>
internal abstract class Constraint
{
    /// <summary>
    /// The builder for the <see cref="Api.Diagnostics.Diagnostic"/>.
    /// </summary>
    public Diagnostic.Builder Diagnostic { get; } = new();
}
