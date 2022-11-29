using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Semantics.Types;
using Draco.RedGreenTree.Attributes;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Internal.Semantics;

[TransformerBase(typeof(Ast), typeof(Ast))]
internal abstract partial class AstTransformerBase
{
    protected ImmutableArray<TElement> TransformImmutableArray<TElement>(
        ImmutableArray<TElement> elements,
        out bool changed)
        where TElement : Ast
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

    protected ImmutableArray<Symbol> TransformImmutableArray(
        ImmutableArray<Symbol> symbols,
        out bool changed)
    {
        changed = false;
        return symbols;
    }

    protected Symbol TransformSymbol(Symbol symbol, out bool changed)
    {
        changed = false;
        return symbol;
    }

    protected Type TransformType(Type type, out bool changed)
    {
        changed = false;
        return type;
    }

    protected ParseTree? TransformParseTree(ParseTree? parseTree, out bool changed)
    {
        changed = false;
        return parseTree;
    }

    protected string TransformString(string str, out bool changed)
    {
        changed = false;
        return str;
    }

    protected object? TransformObject(object? obj, out bool changed)
    {
        changed = false;
        return obj;
    }
}
