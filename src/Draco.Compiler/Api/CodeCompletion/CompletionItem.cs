namespace Draco.Compiler.Api.CodeCompletion;

internal record class CompletionItem(string Text, params Context[] Contexts);
