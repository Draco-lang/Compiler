using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Represents a signature information.
/// </summary>
/// <param name="Overloads">List of all function overloads this <see cref="SignatureItem"/> represents.</param>
/// <param name="CurrentOverload">The function overload that should be currently active based on parameter information.</param>
/// <param name="CurrentParameter">The function parameter that should be currently active based on parameter information.</param>
public sealed record class SignatureItem(ImmutableArray<IFunctionSymbol> Overloads, IFunctionSymbol CurrentOverload, IParameterSymbol? CurrentParameter);
