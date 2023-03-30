using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Fuzzer.Components;

/// <summary>
/// Represents the fuzzer of a compiler component.
/// </summary>
internal interface IComponentFuzzer
{
    /// <summary>
    /// Starts a new epoch for the component.
    /// </summary>
    public void NextEpoct();

    /// <summary>
    /// Mutates the input and feeds it into the component.
    /// </summary>
    public void NextMutation();
}
