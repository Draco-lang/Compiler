using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Some method-local variable allocation.
/// </summary>
/// <param name="Symbol">The corresponding local symbol.</param>
/// <param name="Index">The index of the local within the method.</param>
internal readonly record struct AllocatedLocal(LocalSymbol Symbol, int Index);
