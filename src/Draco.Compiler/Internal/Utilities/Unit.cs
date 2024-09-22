using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// Utility type for returning nothing as an expression.
/// Usable instead of 'void'.
/// </summary>
[ExcludeFromCodeCoverage]
internal readonly record struct Unit;
