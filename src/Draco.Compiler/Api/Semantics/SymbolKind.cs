namespace Draco.Compiler.Api.Semantics;

/// <summary>
/// The different kinds of symbols.
/// </summary>
public enum SymbolKind
{
    Module,
    Field,
    Property,
    Global,
    Local,
    Parameter,
    Function,
    FunctionGroup,
    Type,
    TypeParameter,
    Alias,
    Label,
}
