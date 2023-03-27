using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Model;

/// <summary>
/// Interface for all one-of DUs.
/// </summary>
public interface IOneOf
{
    /// <summary>
    /// The stored value.
    /// </summary>
    public object? Value { get; }
}

public readonly struct OneOf<T1, T2> : IOneOf
{
    public object? Value => this.index switch
    {
        1 => this.field1,
        2 => this.field2,
        _ => throw new InvalidOperationException(),
    };

    private readonly byte index;
    private readonly T1 field1;
    private readonly T2 field2;

    private OneOf(byte index, T1 field1, T2 field2)
    {
        this.index = index;
        this.field1 = field1;
        this.field2 = field2;
    }

    public OneOf(T1 value)
        : this(1, value, default!)
    {
    }

    public OneOf(T2 value)
        : this(2, default!, value)
    {
    }

    public static implicit operator OneOf<T1, T2>(T1 value) => new(value);
    public static implicit operator OneOf<T1, T2>(T2 value) => new(value);

    public override string? ToString() => this.index switch
    {
        1 => this.field1!.ToString(),
        2 => this.field2!.ToString(),
        _ => throw new InvalidOperationException(),
    };
}

public readonly struct OneOf<T1, T2, T3> : IOneOf
{
    public object? Value => this.index switch
    {
        1 => this.field1,
        2 => this.field2,
        3 => this.field3,
        _ => throw new InvalidOperationException(),
    };

    private readonly byte index;
    private readonly T1 field1;
    private readonly T2 field2;
    private readonly T3 field3;

    private OneOf(byte index, T1 field1, T2 field2, T3 field3)
    {
        this.index = index;
        this.field1 = field1;
        this.field2 = field2;
        this.field3 = field3;
    }

    public OneOf(T1 value)
        : this(1, value, default!, default!)
    {
    }

    public OneOf(T2 value)
        : this(2, default!, value, default!)
    {
    }

    public OneOf(T3 value)
        : this(3, default!, default!, value)
    {
    }

    public static implicit operator OneOf<T1, T2, T3>(T1 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3>(T2 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3>(T3 value) => new(value);

    public override string? ToString() => this.index switch
    {
        1 => this.field1!.ToString(),
        2 => this.field2!.ToString(),
        3 => this.field3!.ToString(),
        _ => throw new InvalidOperationException(),
    };
}
