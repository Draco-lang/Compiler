using System;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Declarations;

/// <summary>
/// Represents a portion of a module that was read up from a single file.
/// </summary>
internal sealed class SingleModuleDeclaration : Declaration
{
    /// <summary>
    /// The syntax node of this module portion.
    /// </summary>
    public CompilationUnitSyntax Syntax { get; }

    /// <summary>
    /// The path of this module, including the root module.
    /// </summary>
    public SplitPath Path { get; }

    public override ImmutableArray<Declaration> Children => this.children ??= this.BuildChildren();
    private ImmutableArray<Declaration>? children;

    public SingleModuleDeclaration(string name, SplitPath path, CompilationUnitSyntax syntax)
        : base(name)
    {
        this.Syntax = syntax;
        this.Path = path;
    }

    private ImmutableArray<Declaration> BuildChildren() =>
        this.Syntax.Declarations.Select(BuildChild).OfType<Declaration>().ToImmutableArray();

    private static Declaration? BuildChild(SyntaxNode node)
    {
        switch (node)
        {
        // NOTE: We ignore import declarations in the declaration tree, unlike Roslyn
        // We handle import declarations during constructing the binders
        // Since we allow for imports in local scopes too, this is the most sensible choice
        case ImportDeclarationSyntax:
        case UnexpectedDeclarationSyntax:
            return null;
        case VariableDeclarationSyntax var:
            return new GlobalDeclaration(var);
        case FunctionDeclarationSyntax func:
            return new FunctionDeclaration(func);
        default:
            throw new ArgumentOutOfRangeException(nameof(node)),
        }
    }
}
