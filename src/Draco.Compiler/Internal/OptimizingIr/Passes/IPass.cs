using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.OptimizingIr.Passes;

/// <summary>
/// A pass over the IR code.
/// </summary>
internal interface IPass
{
    /// <summary>
    /// Applies the pass to the given <see cref="Assembly"/>.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> to apply the pass to.</param>
    /// <returns>True, if the pass changed something.</returns>
    public bool Apply(Assembly assembly);
}
