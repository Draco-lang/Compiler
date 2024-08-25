using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Syntax;

namespace Draco.Compiler.Internal.Symbols.Script;

/// <summary>
/// A global variable defined inside a script.
///
/// Globals are special in script context, as they can be inferred from other statements,
/// and their initialization intermixes with the rest of the script.
/// </summary>
internal sealed class ScriptGlobalSymbol(
    ScriptModuleSymbol containingSymbol,
    VariableDeclarationSyntax syntax) : SyntaxGlobalSymbol(containingSymbol, syntax)
{
    public override TypeSymbol Type => this.type ??= new TypeVariable(1);
    private TypeSymbol? type;

    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;
}
