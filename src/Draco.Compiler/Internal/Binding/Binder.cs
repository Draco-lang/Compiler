using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Types;

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
    /// The syntax that constructed this binder.
    /// </summary>
    public virtual SyntaxNode? DeclaringSyntax => this.Parent?.DeclaringSyntax;

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

    public BoundStatement BindFunction(SourceFunctionSymbol function, DiagnosticBag diagnostics)
    {
        var functionName = function.DeclarationSyntax.Name.Text;
        var constraints = new ConstraintSolver(function.DeclarationSyntax, $"function {functionName}");
        var untypedStatement = this.BindStatement(function.DeclarationSyntax.Body, constraints, diagnostics);
        constraints.Solve(diagnostics);
        var boundStatement = this.TypeStatement(untypedStatement, constraints, diagnostics);
        return boundStatement;
    }

    public (Type Type, BoundExpression? Value) BindGlobal(SourceGlobalSymbol global, DiagnosticBag diagnostics)
    {
        var globalName = global.DeclarationSyntax.Name.Text;
        var constraints = new ConstraintSolver(global.DeclarationSyntax, $"global {globalName}");

        var typeSyntax = global.DeclarationSyntax.Type;
        var valueSyntax = global.DeclarationSyntax.Value;

        // Bind type and value
        var type = typeSyntax is null ? null : this.BindType(typeSyntax.Type, diagnostics);
        var untypedValue = valueSyntax is null ? null : this.BindExpression(valueSyntax.Value, constraints, diagnostics);

        // Infer declared type
        var declaredType = (type as TypeSymbol)?.Type ?? constraints.NextTypeVariable;

        // Add assignability constraint, if needed
        if (untypedValue is not null)
        {
            constraints
                .Assignable(declaredType, untypedValue.TypeRequired)
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(global.DeclarationSyntax.Value!.Value.Location));
        }

        // Solve
        constraints.Solve(diagnostics);

        // Type out the expression, if needed
        var boundValue = untypedValue is null ? null : this.TypeExpression(untypedValue, constraints, diagnostics);

        // Unwrap the type
        declaredType = constraints.Unwrap(declaredType);

        if (declaredType.IsTypeVariable)
        {
            // We could not infer the type
            diagnostics.Add(Diagnostic.Create(
                template: TypeCheckingErrors.CouldNotInferType,
                location: global.DeclarationSyntax.Location,
                formatArgs: global.Name));
            // We use an error type
            declaredType = IntrinsicTypes.Error;
        }

        // Done
        return (declaredType, boundValue);
    }
}
