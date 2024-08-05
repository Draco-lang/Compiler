using System.Collections.Immutable;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// An intrinsic to provide the default value of a type (like `default(T)` in C#).
///
/// Signature:
///     func default<T>(): T;
/// </summary>
internal sealed class DefaultValueFunctionSymbol : FunctionSymbol
{
    /// <summary>
    /// A singleton instance of the default value function intrinsic.
    /// </summary>
    public static DefaultValueFunctionSymbol Instance { get; } = new();

    public override string Name => "default";
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;

    public override ImmutableArray<TypeParameterSymbol> GenericParameters =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, this.BuildGenericParameters);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override ImmutableArray<ParameterSymbol> Parameters => [];

    public override TypeSymbol ReturnType => this.GenericParameters[0];

    public override CodegenDelegate Codegen => (codegen, targetType, args) => new DefaultValue(targetType);

    private DefaultValueFunctionSymbol()
    {
    }

    private ImmutableArray<TypeParameterSymbol> BuildGenericParameters() => [
        new SynthetizedTypeParameterSymbol(this, "T"),
    ];
}
