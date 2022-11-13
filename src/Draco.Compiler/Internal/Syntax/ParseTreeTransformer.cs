using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;
using Draco.RedGreenTree.Attributes;
using static Draco.Compiler.Internal.Syntax.ParseTree;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Provides a transformer base that transforms the tree.
/// </summary>
internal abstract partial class ParseTreeTransformerBase
{
    protected ImmutableArray<TElement> TransformImmutableArray<TElement>(
        ImmutableArray<TElement> elements,
        out bool changed)
        where TElement : ParseTree
    {
        // TODO
        throw new NotImplementedException();
    }

    protected virtual ImmutableArray<Diagnostic> TransformImmutableArray(
        ImmutableArray<Diagnostic> diags,
        out bool changed)
    {
        changed = false;
        return diags;
    }

    protected PunctuatedList<TElement> TransformPunctuatedList<TElement>(
        PunctuatedList<TElement> list,
        out bool changed)
        where TElement : ParseTree
    {
        // TODO
        throw new NotImplementedException();
    }

    protected Punctuated<TElement> TransformPunctuated<TElement>(
        Punctuated<TElement> punctuated,
        out bool changed)
        where TElement : ParseTree
    {
        // TODO
        throw new NotImplementedException();
    }

    protected Enclosed<TElement> TransformEnclosed<TElement>(
        Enclosed<TElement> enclosed,
        out bool changed)
        where TElement : ParseTree
    {
        // TODO
        throw new NotImplementedException();
    }

    protected Enclosed<PunctuatedList<TElement>> TransformEnclosed<TElement>(
        Enclosed<PunctuatedList<TElement>> enclosed,
        out bool changed)
        where TElement : ParseTree
    {
        // TODO
        throw new NotImplementedException();
    }

    public virtual Token TransformToken(Token token, out bool changed)
    {
        changed = false;
        return token;
    }
}
