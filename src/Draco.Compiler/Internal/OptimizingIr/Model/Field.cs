using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

internal sealed class Field(FieldSymbol symbol, Type declaringType) : IField
{
    public FieldSymbol Symbol { get; } = symbol;

    public string Name => this.Symbol.Name;

    public Type DeclaringType { get; } = declaringType;
    IType IField.DeclaringType => this.DeclaringType;

    public IReadOnlyList<AttributeInstance> Attributes => this.Symbol.Attributes;

    public TypeSymbol Type => this.Symbol.Type;
}
