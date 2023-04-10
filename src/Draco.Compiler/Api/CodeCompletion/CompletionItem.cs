namespace Draco.Compiler.Api.CodeCompletion;

public record class CompletionItem(string Text, CompletionKind Kind, params CompletionContext[] Contexts);
