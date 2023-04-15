using System.Collections.Immutable;

namespace Draco.Compiler.Api.CodeCompletion;

public record class CompletionItem(string Text, CompletionKind Kind, string? Type, string? Documentation, params CompletionContext[] Context);

public record class SignatureItem(string Label, string Documentation, ImmutableArray<string> Parameters);

public record class SignatureCollection(ImmutableArray<SignatureItem> Signatures, int ActiveOverload, int? ActiveParameter);
