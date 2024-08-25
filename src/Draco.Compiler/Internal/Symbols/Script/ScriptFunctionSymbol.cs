using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols.Syntax;

namespace Draco.Compiler.Internal.Symbols.Script;

/// <summary>
/// A function defined inside a script.
/// </summary>
internal sealed class ScriptFunctionSymbol(
    Symbol containingSymbol,
    FunctionDeclarationSyntax syntax) : SyntaxFunctionSymbol(containingSymbol, syntax)
{
    public override BoundStatement Body => throw new System.NotImplementedException();
    public override Visibility Visibility => Visibility.Public;
}
