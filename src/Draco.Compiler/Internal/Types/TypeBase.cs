using System.Collections.Generic;
using System.Linq;

namespace Draco.Compiler.Internal.Types;

internal sealed class TypeBase
{
    private readonly IEnumerable<Type> types;

    private TypeBase(IEnumerable<Type> types)
    {
        this.types = types;
    }

    public bool Contains(Type type) => this.types.Contains(type);

    public static TypeBase Integral { get; } = new TypeBase(new Type[]
    {
        IntrinsicTypes.Int8,
        IntrinsicTypes.Int16,
        IntrinsicTypes.Int32,
        IntrinsicTypes.Int64,

        IntrinsicTypes.Uint8,
        IntrinsicTypes.Uint16,
        IntrinsicTypes.Uint32,
        IntrinsicTypes.Uint64,
    });

    public static TypeBase FloatingPoint { get; } = new TypeBase(new Type[]
    {
        IntrinsicTypes.Float32,
        IntrinsicTypes.Float64,
    });
}
