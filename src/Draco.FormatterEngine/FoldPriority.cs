namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// The priority when folding a scope.
/// </summary>
public enum FoldPriority
{
    /// <summary>
    /// The scope will never be folded. Mostly used for scopes that are already folded.
    /// </summary>
    Never,
    /// <summary>
    /// This scope will be folded as late as possible.
    /// </summary>
    AsLateAsPossible,
    /// <summary>
    /// This scope will be folded as soon as possible.
    /// </summary>
    AsSoonAsPossible
}
