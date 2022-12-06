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

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

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
}
