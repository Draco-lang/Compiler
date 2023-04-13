using System.Collections.Immutable;

namespace Draco.Compiler.Api.CodeCompletion;

public record class SignatureItem(string Label, string Documentation, ImmutableArray<string> Parameters);
