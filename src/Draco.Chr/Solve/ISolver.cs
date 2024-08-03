using Draco.Chr.Constraints;

namespace Draco.Chr.Solve;

/// <summary>
/// Represents a CHR solver.
/// </summary>
public interface ISolver
{
    /// <summary>
    /// Solves the given store and returns the result.
    /// </summary>
    /// <param name="store">The initial constraint store.</param>
    /// <returns>The solved constraint store. Can be the same mutated instance as <paramref name="store"/>.</returns>
    public ConstraintStore Solve(ConstraintStore store);
}
