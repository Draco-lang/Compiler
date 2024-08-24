using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Symbols.Script;

/// <summary>
/// A global variable defined inside a script.
///
/// Globals are special in script context, as they can be inferred from other statements,
/// and their initialization intermixes with the rest of the script.
/// </summary>
internal sealed class ScriptGlobalSymbol(
    ScriptModuleSymbol containingSymbol,
    VariableDeclarationSyntax syntax) : GlobalSymbol, ISourceSymbol
{
    public override TypeSymbol Type => throw new System.NotImplementedException();

    public override bool IsMutable => this.DeclaringSyntax.Keyword.Kind == TokenKind.KeywordVar;
    public override ScriptModuleSymbol ContainingSymbol => containingSymbol;
    public override string Name => this.DeclaringSyntax.Name.Text;
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;

    public override VariableDeclarationSyntax DeclaringSyntax => syntax;

    public void Bind(IBinderProvider binderProvider) => throw new System.NotImplementedException();
}
