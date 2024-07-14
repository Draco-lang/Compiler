using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.OverloadResolution;

/// <summary>
/// Represents an argument to a function.
/// </summary>
/// <param name="Syntax">The syntax of the argument.</param>
/// <param name="Type">The type of the argument.</param>
internal readonly record struct Argument(SyntaxNode? Syntax, TypeSymbol Type);
