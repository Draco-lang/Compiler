using System.Collections.Immutable;
using Draco.Compiler.Internal.Diagnostics;
using Draco.RedGreenTree.Attributes;
using static Draco.Compiler.Internal.Syntax.ParseNode;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Provides a visitor base that visits each child of the tree.
/// When overriding a method, make sure to call the base method or explicitly visit children
/// to recurse in the tree.
/// </summary>
/// <typeparam name="T">The return type of the visitor.</typeparam>
[VisitorBase(typeof(ParseNode), typeof(ParseNode))]
internal abstract partial class ParseTreeVisitorBase<T>
{
    protected T VisitImmutableArray<TElement>(ImmutableArray<TElement> elements)
        where TElement : ParseNode
    {
        foreach (var item in elements) this.Visit(item);
        return this.Default;
    }

    protected virtual T VisitImmutableArray(ImmutableArray<Diagnostic> diags) => this.Default;

    protected T VisitPunctuatedList<TElement>(PunctuatedList<TElement> list)
        where TElement : ParseNode
    {
        foreach (var item in list.Elements) this.VisitPunctuated(item);
        return this.Default;
    }

    protected T VisitPunctuated<TElement>(Punctuated<TElement> punctuated)
        where TElement : ParseNode
    {
        this.Visit(punctuated.Value);
        if (punctuated.Punctuation is not null) this.VisitToken(punctuated.Punctuation);
        return this.Default;
    }

    protected T VisitEnclosed<TElement>(Enclosed<TElement> enclosed)
        where TElement : ParseNode
    {
        this.VisitToken(enclosed.OpenToken);
        this.Visit(enclosed.Value);
        this.VisitToken(enclosed.CloseToken);
        return this.Default;
    }

    protected T VisitEnclosed<TElement>(Enclosed<PunctuatedList<TElement>> enclosed)
        where TElement : ParseNode
    {
        this.VisitToken(enclosed.OpenToken);
        this.VisitPunctuatedList(enclosed.Value);
        this.VisitToken(enclosed.CloseToken);
        return this.Default;
    }

    public virtual T VisitToken(Token token)
    {
        this.VisitImmutableArray(token.Diagnostics);
        return this.Default;
    }
}
