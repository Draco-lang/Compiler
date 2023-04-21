namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// The kind of completion that will be inserted into code if this <see cref="CompletionItem"/> is applied.
/// </summary>
public enum CompletionKind
{
    Variable,
    Function,
    Class,
    Module,
    Keyword,
}
