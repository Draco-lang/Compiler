using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// The result of binding a global variable.
/// </summary>
/// <param name="Type">The type of the global variable.</param>
/// <param name="Value">The value of the global variable, if any.</param>
internal readonly record struct GlobalBinding(TypeSymbol Type, BoundExpression? Value);
