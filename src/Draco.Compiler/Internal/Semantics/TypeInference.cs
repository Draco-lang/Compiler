using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// A visitor that does type-inference on the given subtree.
/// </summary>
internal sealed class TypeInferenceVisitor : ParseTreeVisitorBase<Unit>
{
    public ImmutableDictionary<Symbol, Type> Result => this.types.ToImmutable();

    private readonly ImmutableDictionary<Symbol, Type>.Builder types = ImmutableDictionary.CreateBuilder<Symbol, Type>();
}
