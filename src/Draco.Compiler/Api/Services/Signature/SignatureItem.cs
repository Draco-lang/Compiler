using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Api.Services.Signature;

/// <summary>
/// Represents signature information about a set of overloads.
/// </summary>
public sealed class SignatureItem(
    ImmutableArray<IFunctionSymbol> overloads,
    IFunctionSymbol bestMatch,
    IParameterSymbol? currentParameter)
{
    /// <summary>
    /// The function overloads that the signature is trying to resolve.
    /// </summary>
    public ImmutableArray<IFunctionSymbol> Overloads { get; } = overloads;

    /// <summary>
    /// The currently best matching overload based on the parameters.
    /// </summary>
    public IFunctionSymbol BestMatch { get; } = bestMatch;

    /// <summary>
    /// The currently active parameter.
    /// </summary>
    public IParameterSymbol? CurrentParameter { get; } = currentParameter;
}
