using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Diagnostics;
using Draco.Compiler.Utilities;
using static Draco.Compiler.Syntax.ParseTree;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Defines a visitor for <see cref="ParseTree"/>s.
/// A visitor recursively visits all tree elements.
/// </summary>
/// <typeparam name="T">The return type of the visitor methods.</typeparam>
[Draco.RedGreenTree.VisitorInterface(typeof(ParseTree))]
internal partial interface IParseTreeVisitor<out T>
{
    public T VisitToken(Token token);
}

/// <summary>
/// Provides a base implementation of <see cref="IParseTreeVisitor{T}"/> that
/// visits each child of the tree.
/// When overriding a method, make sure to call the base method or explicitly visit children
/// to recurse in the tree.
/// </summary>
/// <typeparam name="T">The return type of the visitor.</typeparam>
[Draco.RedGreenTree.VisitorBase(typeof(ParseTree))]
internal abstract partial class ParseTreeVisitorBase<T> : IParseTreeVisitor<T>
{
    protected T VisitValueArray<TElement>(ValueArray<TElement> elements)
        where TElement : ParseTree
    {
        foreach (var item in elements) this.Visit(item);
        return this.Default;
    }

    protected virtual T VisitValueArray(ValueArray<Diagnostic> diags) => this.Default;

    protected T VisitPunctuatedList<TElement>(PunctuatedList<TElement> list)
        where TElement : ParseTree
    {
        foreach (var item in list.Elements) this.VisitPunctuated(item);
        return this.Default;
    }

    protected T VisitPunctuated<TElement>(Punctuated<TElement> punctuated)
        where TElement : ParseTree
    {
        this.Visit(punctuated.Value);
        if (punctuated.Punctuation is not null) this.VisitToken(punctuated.Punctuation);
        return this.Default;
    }

    protected T VisitEnclosed<TElement>(Enclosed<TElement> enclosed)
        where TElement : ParseTree
    {
        this.VisitToken(enclosed.OpenToken);
        this.Visit(enclosed.Value);
        this.VisitToken(enclosed.CloseToken);
        return this.Default;
    }

    protected T VisitEnclosed<TElement>(Enclosed<PunctuatedList<TElement>> enclosed)
        where TElement : ParseTree
    {
        this.VisitToken(enclosed.OpenToken);
        this.VisitPunctuatedList(enclosed.Value);
        this.VisitToken(enclosed.CloseToken);
        return this.Default;
    }

    public virtual T VisitToken(Token token) => this.Default;
}
