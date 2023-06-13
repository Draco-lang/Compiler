namespace Draco.Compiler.Api.Syntax;

public abstract partial class ContainerSyntax : DeclarationSyntax
{
    /// <summary>
    /// All declaration syntaxes within the container.
    /// </summary>
    public abstract SyntaxList<DeclarationSyntax> Declarations { get; }
}
