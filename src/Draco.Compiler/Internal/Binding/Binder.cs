using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
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
    protected Compilation Compilation { get; }

    /// <summary>
    /// The parent binder of this one.
    /// </summary>
    protected Binder? Parent { get; }

    /// <summary>
    /// The symbol containing the binding context.
    /// </summary>
    public virtual Symbol? ContainingSymbol => this.Parent?.ContainingSymbol;

    protected Binder(Compilation compilation)
    {
        this.Compilation = compilation;
    }

    protected Binder(Binder parent)
        : this(parent.Compilation)
    {
        this.Parent = parent;
    }

    public BoundStatement BindFunctionBody(FunctionBodySyntax syntax)
    {
        var constraints = new ConstraintBag();
        var untypedStatement = this.BindStatement(syntax, constraints);
        var boundStatement = this.TypeStatement(untypedStatement);
        return boundStatement;
    }
}
