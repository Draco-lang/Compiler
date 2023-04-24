using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// </summary>
    Advanced,

    /// <summary>
    /// The constraint got solved.
    /// </summary>
    Solved,
}
