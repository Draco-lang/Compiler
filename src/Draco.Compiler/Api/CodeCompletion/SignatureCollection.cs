using System.Collections.Immutable;

namespace Draco.Compiler.Api.CodeCompletion;

public record class SignatureCollection(ImmutableArray<SignatureItem> Signatures, int ActiveOverload, int? ActiveParameter);
