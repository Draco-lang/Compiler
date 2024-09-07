using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A function intrinsic known by the compiler. This function has no implementation, as it is known by the compiler.
/// </summary>
internal sealed class IntrinsicFunctionSymbol : FunctionSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters { get; }

    public override TypeSymbol ReturnType { get; }
    public override bool IsSpecialName => true;
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Private;

    public override string Name { get; }
    public override BoundStatement? Body { get; }

    public IntrinsicFunctionSymbol(
        string name,
        IEnumerable<TypeSymbol> paramTypes,
        TypeSymbol returnType,
        BoundStatement? body = null)
    {
        this.Name = name;
        this.Parameters = paramTypes
            .Select(t => new SynthetizedParameterSymbol(this, t))
            .Cast<ParameterSymbol>()
            .ToImmutableArray();
        this.ReturnType = returnType;
        this.Body = body;
    }
}
