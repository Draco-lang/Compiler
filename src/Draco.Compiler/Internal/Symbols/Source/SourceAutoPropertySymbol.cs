using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols.Syntax;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Auto-property defined based on some source.
/// Currently this class only models global =module-level) auto-properties, which is the equivalent of C# static auto-properties.
/// </summary>
internal sealed class SourceAutoPropertySymbol(
    Symbol containingSymbol,
    VariableDeclarationSyntax syntax) : SyntaxAutoPropertySymbol(containingSymbol, syntax)
{
    public override FunctionSymbol? Getter => throw new System.NotImplementedException();
    public override FunctionSymbol? Setter => throw new System.NotImplementedException();

    public override TypeSymbol Type => throw new System.NotImplementedException();

    public override bool IsStatic => true;

    public override void Bind(IBinderProvider binderProvider) => throw new System.NotImplementedException();
}
