using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Represents a single scope that binds the syntax-tree to the untyped-tree and then the bound-tree.
/// </summary>
internal abstract partial class Binder
{
    /// <summary>
    /// The compilation this binder was created for.
    /// </summary>
    internal Compilation Compilation { get; }

    /// <summary>
    /// The parent binder of this one.
    /// </summary>
    internal Binder? Parent { get; }

    /// <summary>
    /// The symbol containing the binding context.
    /// </summary>
    public virtual Symbol? ContainingSymbol => this.Parent?.ContainingSymbol;

    /// <summary>
    /// The symbols declared in this binder scope.
    /// </summary>
    public virtual IEnumerable<Symbol> DeclaredSymbols => Enumerable.Empty<Symbol>();

    protected Binder(Compilation compilation, Binder? parent)
    {
        this.Compilation = compilation;
        this.Parent = parent;
    }

    protected Binder(Compilation compilation)
        : this(compilation, null)
    {
    }

    protected Binder(Binder parent)
        : this(parent.Compilation, parent)
    {
    }

    /// <summary>
    /// Retrieves the appropriate binder for the given syntax node.
    /// </summary>
    /// <param name="node">The node to retrieve the binder for.</param>
    /// <returns>The appropriate binder for the node.</returns>
    protected virtual Binder GetBinder(SyntaxNode node) =>
        this.Compilation.GetBinder(node);

    public BoundStatement BindFunctionBody(FunctionBodySyntax syntax, DiagnosticBag diagnostics)
    {
        var functionName = ((FunctionDeclarationSyntax)syntax.Parent!).Name.Text;
        var constraints = new ConstraintSolver($"function {functionName}");
        var untypedStatement = this.BindStatement(syntax, constraints, diagnostics);
        constraints.Solve(diagnostics);
        var boundStatement = this.TypeStatement(untypedStatement, constraints, diagnostics);
        return boundStatement;
    }

    public BoundExpression BindGlobalValue(ExpressionSyntax syntax, DiagnosticBag diagnostics)
    {
        var globalName = ((VariableDeclarationSyntax)syntax.Parent!.Parent!).Name.Text;
        var constraints = new ConstraintSolver($"global {globalName}");
        var untypedExpression = this.BindExpression(syntax, constraints, diagnostics);
        constraints.Solve(diagnostics);
        var boundExpression = this.TypeExpression(untypedExpression, constraints, diagnostics);
        return boundExpression;
    }
}
