using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Utilities;

/// <summary>
/// Represents an argument for a call.
/// </summary>
/// <param name="Syntax">The syntax of the argument, if any.</param>
/// <param name="Type">The type of the argument.</param>
internal readonly record struct ArgumentDescription(SyntaxNode? Syntax, TypeSymbol Type);
