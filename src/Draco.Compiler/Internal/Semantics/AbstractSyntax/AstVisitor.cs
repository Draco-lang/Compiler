using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.RedGreenTree.Attributes;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

[VisitorBase(typeof(Ast), typeof(Ast))]
internal abstract partial class AstVisitorBase<T>
{
    protected T VisitImmutableArray(ImmutableArray<Ast.Decl> decls)
    {
        foreach (var item in decls) this.VisitDecl(item);
        return this.Default;
    }

    protected T VisitImmutableArray(ImmutableArray<Ast.Stmt> stmts)
    {
        foreach (var item in stmts) this.VisitStmt(item);
        return this.Default;
    }

    protected T VisitImmutableArray(ImmutableArray<Ast.Expr> exprs)
    {
        foreach (var item in exprs) this.VisitExpr(item);
        return this.Default;
    }

    protected T VisitImmutableArray(ImmutableArray<Ast.ComparisonElement> cmps)
    {
        foreach (var item in cmps) this.VisitExpr(item.Right);
        return this.Default;
    }

    protected T VisitImmutableArray(ImmutableArray<Ast.StringPart> parts)
    {
        foreach (var item in parts) this.VisitStringPart(item);
        return this.Default;
    }

    protected T VisitImmutableArray(ImmutableArray<Symbol> symbols) => this.Default;
    protected T VisitImmutableArray(ImmutableArray<Symbol.Parameter> symbols) => this.Default;
}
