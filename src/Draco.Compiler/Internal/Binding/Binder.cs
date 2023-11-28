using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;

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
    /// Utility accessor for intrinsics.
    /// </summary>
    protected IntrinsicSymbols IntrinsicSymbols => this.Compilation.IntrinsicSymbols;

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

    public virtual BoundStatement BindFunction(SourceFunctionSymbol function, DiagnosticBag diagnostics)
    {
        var functionName = function.DeclaringSyntax.Name.Text;
        var constraints = new ConstraintSolver(function.DeclaringSyntax, $"function {functionName}");
        var statementTask = this.BindStatement(function.DeclaringSyntax.Body, constraints, diagnostics);
        constraints.Solve(diagnostics);
        return statementTask.Result;
    }

    public virtual (TypeSymbol Type, BoundExpression? Value) BindGlobal(SourceGlobalSymbol global, DiagnosticBag diagnostics)
    {
        var globalName = global.DeclaringSyntax.Name.Text;
        var constraints = new ConstraintSolver(global.DeclaringSyntax, $"global {globalName}");

        var typeSyntax = global.DeclaringSyntax.Type;
        var valueSyntax = global.DeclaringSyntax.Value;

        // Bind type and value
        var type = typeSyntax is null ? null : this.BindTypeToTypeSymbol(typeSyntax.Type, diagnostics);
        var valueTask = valueSyntax is null
            ? null
            : this.BindExpression(valueSyntax.Value, constraints, diagnostics);

        // Infer declared type
        var declaredType = type ?? constraints.AllocateTypeVariable(track: false);

        // Add assignability constraint, if needed
        if (valueTask is not null)
        {
            _ = constraints.Assignable(
                declaredType,
                valueTask.GetResultType(valueSyntax, constraints, diagnostics),
                global.DeclaringSyntax.Value!.Value);
        }

        // Solve
        constraints.Solve(diagnostics);

        // Type out the expression, if needed
        var boundValue = valueTask?.Result;

        // Unwrap the type
        declaredType = declaredType.Substitution;

        if (declaredType.IsTypeVariable)
        {
            // We could not infer the type
            diagnostics.Add(Diagnostic.Create(
                template: TypeCheckingErrors.CouldNotInferType,
                location: global.DeclaringSyntax.Location,
                formatArgs: global.Name));
            // We use an error type
            declaredType = IntrinsicSymbols.ErrorType;
        }

        // Done
        return (declaredType, boundValue);
    }

    // NOTE: This is a hack, until we find out something nicer
    // We essentially use this to notify incremental binder that a left-hand side of a module or a type access
    // will be erased, won't be present in the bound tree.
    // Once we start modeling module member access without throwing it away, we can get rid of it.
    // In addition, this is used by for-loops too to associate the iterator with its symbol
    internal virtual void BindSyntaxToSymbol(SyntaxNode syntax, Symbol module) { }
    internal virtual void BindTypeSyntaxToSymbol(SyntaxNode syntax, TypeSymbol type) { }

    private static FunctionSymbol GetGetterSymbol(SyntaxNode? syntax, PropertySymbol prop, DiagnosticBag diags)
    {
        var result = prop.Getter;
        if (result is null)
        {
            diags.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.CannotGetSetOnlyProperty,
                location: syntax?.Location,
                prop.FullName));
            result = new UndefinedPropertyAccessorSymbol(prop);
        }
        return result;
    }

    private static FunctionSymbol GetSetterSymbol(SyntaxNode? syntax, PropertySymbol prop, DiagnosticBag diags)
    {
        var result = prop.Setter;
        if (result is null)
        {
            diags.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.CannotSetGetOnlyProperty,
                location: syntax?.Location,
                prop.FullName));
            result = new UndefinedPropertyAccessorSymbol(prop);
        }
        return result;
    }
}
