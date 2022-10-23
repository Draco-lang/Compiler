using System;
using System.Collections.Generic;
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
        ValueArray<Punctuated<T>> Elements);

}
