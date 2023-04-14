namespace Draco.Compiler.Api.CodeCompletion;

public record class CompletionItem(string Text, CompletionKind Kind, string? Type, string? Documentation, params CompletionContext[] Context);
