namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Categories for <see cref="CompletionItem"/>s that can be used to categorize the completions.
/// </summary>
public enum CompletionKind
{
    Variable,
    Function,
    Class,
    Module,
    Property,
    Keyword,
}
