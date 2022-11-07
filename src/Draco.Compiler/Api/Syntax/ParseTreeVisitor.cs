using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.RedGreenTree.Attributes;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Provides a visitor base that visits each child of the tree.
/// When overriding a method, make sure to call the base method or explicitly visit children
/// to recurse in the tree.
/// </summary>
/// <typeparam name="T">The return type of the visitor.</typeparam>
[VisitorBase(typeof(Internal.Syntax.ParseTree), typeof(ParseTree))]
public abstract partial class ParseTreeVisitorBase<T>
{
    protected T VisitImmutableArray<TElement>(ImmutableArray<TElement> elements)
        where TElement : ParseTree
    {
        foreach (var item in elements) this.Visit(item);
        return this.Default;
    }

    protected virtual T VisitImmutableArray(ImmutableArray<Diagnostic> diags) => this.Default;

    protected T VisitPunctuatedList<TElement>(ParseTree.PunctuatedList<TElement> list)
        where TElement : ParseTree
    {
        foreach (var item in list.Elements) this.VisitPunctuated(item);
        return this.Default;
    }

    protected T VisitPunctuated<TElement>(ParseTree.Punctuated<TElement> punctuated)
        where TElement : ParseTree
    {
        this.Visit(punctuated.Value);
        if (punctuated.Punctuation is not null) this.VisitToken(punctuated.Punctuation);
        return this.Default;
    }

    protected T VisitEnclosed<TElement>(ParseTree.Enclosed<TElement> enclosed)
        where TElement : ParseTree
    {
        this.VisitToken(enclosed.OpenToken);
        this.Visit(enclosed.Value);
        this.VisitToken(enclosed.CloseToken);
        return this.Default;
    }

    protected T VisitEnclosed<TElement>(ParseTree.Enclosed<ParseTree.PunctuatedList<TElement>> enclosed)
        where TElement : ParseTree
    {
        this.VisitToken(enclosed.OpenToken);
        this.VisitPunctuatedList(enclosed.Value);
        this.VisitToken(enclosed.CloseToken);
        return this.Default;
    }

    public virtual T VisitToken(ParseTree.Token token) => this.Default;
}
