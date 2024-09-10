using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// An instruction that produces a result in a register.
/// </summary>
internal interface IValueInstruction : IInstruction
{
    /// <summary>
    /// The register to store the result at.
    /// </summary>
    public Register Target { get; }
}
