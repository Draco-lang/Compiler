using System;
using System.Collections.Generic;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint, that runs when another process has finished.
/// </summary>
/// <typeparam name="TResult">The result of this constraint.</typeparam>
internal sealed class AwaitConstraint<TResult> : Constraint<TResult>
{
    /// <summary>
    /// When true, we execute <see cref="Map"/>.
    /// </summary>
    public Func<bool> Awaited { get; }

    /// <summary>
    /// The mapping function that runs when <see cref="Awaited"/> is true.
    /// </summary>
    public Func<TResult> Map { get; }

    public AwaitConstraint(Func<bool> awaited, Func<TResult> map)
    {
        this.Awaited = awaited;
        this.Map = map;
    }

    public override string ToString() => $"Await({this.Awaited})";
}
