namespace Draco.Compiler.Api.Semantics;

/// <summary>
/// The different kinds of symbols.
/// </summary>
public enum SymbolKind
{
    Module,
    Field,
    Property,
    Local,
    Parameter,
    Function,
    FunctionGroup,
    Type,
    TypeParameter,
    Alias,
    Label,
}
