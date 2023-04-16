using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Api.CodeCompletion;

public record class SignatureItem(ImmutableArray<IFunctionSymbol> Overloads, IFunctionSymbol CurrentOverload, IParameterSymbol? CurrentParameter);
