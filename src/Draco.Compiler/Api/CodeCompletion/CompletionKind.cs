namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// The kind of symbol that will be inserted into code if this completion is applied.
/// </summary>
public enum CompletionKind
{
    Variable,
    Function,
    Class,
    Module,
    Keyword,
}
