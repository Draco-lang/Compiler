using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Any variable-like symbol.
/// </summary>
internal abstract partial class VariableSymbol : Symbol, ITypedSymbol
{
    /// <summary>
    /// The type of the local.
    /// </summary>
    public abstract TypeSymbol Type { get; }

    /// <summary>
    /// True, if this local is mutable.
    /// </summary>
    public abstract bool IsMutable { get; }

    public override Api.Semantics.VisibilityType Visibility
    {
        get
        {
            var syntax = this.DeclaringSyntax as VariableDeclarationSyntax;
            if (syntax is null) return Api.Semantics.VisibilityType.Public; // Default
            if (syntax.VisibilityModifier is null) return Api.Semantics.VisibilityType.Private;
            return syntax.VisibilityModifier.Kind == TokenKind.KeywordInternal ? Api.Semantics.VisibilityType.Internal : Api.Semantics.VisibilityType.Public;
        }
    }
}
