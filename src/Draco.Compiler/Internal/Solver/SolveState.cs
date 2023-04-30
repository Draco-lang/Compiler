namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// The state of a constraint after a solve iteration.
/// </summary>
internal enum SolveState
{
    /// <summary>
    /// The constraint could not advance at all.
    /// </summary>
    Stale,

    /// <summary>
    /// The constraint advanced, but did not fully solve.
    /// The solver should reattempt solving the constraint immediately.
    /// </summary>
    AdvancedContinue,

    /// <summary>
    /// The constraint advanced, but did not fully solve.
    /// The solver should not reattempt solving the constraint immeditely, as there
    /// is likely no advancements until other constraints are solved.
    /// </summary>
    AdvancedBreak,

    /// <summary>
    /// The constraint got solved.
    /// </summary>
    Solved,
}
