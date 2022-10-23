using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;
using Draco.RedGreenTree.Attributes;

namespace Draco.Compiler.Api.Syntax;

[RedTree(typeof(Internal.Syntax.ParseTree))]
public abstract partial record class ParseTree
{
    private readonly Internal.Syntax.ParseTree green;
    public ParseTree? Parent { get; }
}

public abstract partial record class ParseTree
{
    public readonly record struct Enclosed<T>(
        Token OpenToken,
        T Value,
        Token CloseToken);

    public readonly record struct Punctuated<T>(
        T Value,
        Token? Punctuation);

    public readonly record struct PunctuatedList<T>(
        ImmutableArray<Punctuated<T>> Elements);

    [return: NotNullIfNotNull(nameof(token))]
    private static Token? ToRed(ParseTree? parent, Internal.Syntax.Token? token) => token is null
        ? null
        : new();

    private static ImmutableArray<Token> ToRed(ParseTree? parent, ImmutableArray<Internal.Syntax.Token> elements) =>
        elements.Select(e => ToRed(parent, e)).ToImmutableArray();

    private static ImmutableArray<StringPart> ToRed(ParseTree? parent, ImmutableArray<Internal.Syntax.ParseTree.StringPart> elements) =>
        elements.Select(e => (StringPart)ToRed(parent, e)).ToImmutableArray();

#if false
    private static ImmutableArray<TElement> ToRed<TElement>(ParseTree? parent, TElement? element)
        where TElement : Internal.Syntax.ParseTree => throw new NotImplementedException();
#endif
}
