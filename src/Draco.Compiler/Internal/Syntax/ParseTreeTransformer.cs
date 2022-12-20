using System.Collections.Immutable;
using Draco.Compiler.Internal.Diagnostics;
using Draco.RedGreenTree.Attributes;
using static Draco.Compiler.Internal.Syntax.ParseTree;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Provides a transformer base that transforms the tree.
/// </summary>
[TransformerBase(typeof(ParseTree), typeof(ParseTree))]
internal abstract partial class ParseTreeTransformerBase
{
    protected ImmutableArray<TElement> TransformImmutableArray<TElement>(
        ImmutableArray<TElement> elements,
        out bool changed)
        where TElement : ParseTree
    {
        changed = false;
        var newBuilder = ImmutableArray.CreateBuilder<TElement>();
        foreach (var element in elements)
        {
            var transformed = (TElement)this.Transform(element, out var elementChanged);
            newBuilder.Add(transformed);
            changed = changed || elementChanged;
        }
        return newBuilder.ToImmutable();
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
        changed = false;
        var newBuilder = ImmutableArray.CreateBuilder<Punctuated<TElement>>();
        foreach (var element in list.Elements)
        {
            var transformed = this.TransformPunctuated(element, out var elementChanged);
            newBuilder.Add(transformed);
            changed = changed || elementChanged;
        }
        return new(newBuilder.ToImmutable());
    }

    protected Punctuated<TElement> TransformPunctuated<TElement>(
        Punctuated<TElement> punctuated,
        out bool changed)
        where TElement : ParseTree
    {
        var punctChanged = false;
        var element = (TElement)this.Transform(punctuated.Value, out var valueChanged);
        var punct = punctuated.Punctuation is null ? null : this.TransformToken(punctuated.Punctuation, out punctChanged);
        changed = valueChanged || punctChanged;
        return new(element, punct);
    }

    protected Enclosed<TElement> TransformEnclosed<TElement>(
        Enclosed<TElement> enclosed,
        out bool changed)
        where TElement : ParseTree
    {
        var open = this.TransformToken(enclosed.OpenToken, out var openChanged);
        var value = (TElement)this.Transform(enclosed.Value, out var valueChanged);
        var close = this.TransformToken(enclosed.CloseToken, out var closeChanged);
        changed = openChanged || valueChanged || closeChanged;
        return new(open, value, close);
    }

    protected Enclosed<PunctuatedList<TElement>> TransformEnclosed<TElement>(
        Enclosed<PunctuatedList<TElement>> enclosed,
        out bool changed)
        where TElement : ParseTree
    {
        var open = this.TransformToken(enclosed.OpenToken, out var openChanged);
        var value = this.TransformPunctuatedList(enclosed.Value, out var valueChanged);
        var close = this.TransformToken(enclosed.CloseToken, out var closeChanged);
        changed = openChanged || valueChanged || closeChanged;
        return new(open, value, close);
    }

    protected int TransformInt32(int value, out bool changed)
    {
        changed = false;
        return value;
    }

    public virtual Token TransformToken(Token token, out bool changed)
    {
        changed = false;
        return token;
    }

    public virtual Trivia TransformTrivia(Trivia trivia, out bool changed)
    {
        changed = false;
        return trivia;
    }
}
