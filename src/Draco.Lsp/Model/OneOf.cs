using System;
using System.Diagnostics.CodeAnalysis;

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

/// <summary>
/// A discriminated union implementation for 1 case(s).
/// </summary>
public readonly struct OneOf<T1> : IOneOf
{
    object? IOneOf.Value => this.index switch
    {
        1 => this.field1,
        _ => throw new InvalidOperationException(),
    };
    private readonly byte index;
    private readonly T1 field1;
    private OneOf(byte index, T1 field1)
    {
        this.index = index;
        this.field1 = field1;
    }

    public OneOf(T1 value) : this(1, value)
    {
    }

    public static implicit operator OneOf<T1>(T1 value) => new(value);
    public T As<T>() => this.Is<T>(out var value) ? value : throw new InvalidCastException();
    public bool Is<T>() => this.Is<T>(out _);
    public bool Is<T>([MaybeNullWhen(false)] out T value)
    {
        if (typeof(T) == typeof(T1))
        {
            if (this.index == 1)
            {
                value = (T)(object)this.field1!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    public override string? ToString() => (this as IOneOf).Value?.ToString();
}

/// <summary>
/// A discriminated union implementation for 2 case(s).
/// </summary>
public readonly struct OneOf<T1, T2> : IOneOf
{
    object? IOneOf.Value => this.index switch
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

    public OneOf(T1 value) : this(1, value, default!)
    {
    }

    public OneOf(T2 value) : this(2, default!, value)
    {
    }

    public static implicit operator OneOf<T1, T2>(T1 value) => new(value);
    public static implicit operator OneOf<T1, T2>(T2 value) => new(value);
    public T As<T>() => this.Is<T>(out var value) ? value : throw new InvalidCastException();
    public bool Is<T>() => this.Is<T>(out _);
    public bool Is<T>([MaybeNullWhen(false)] out T value)
    {
        if (typeof(T) == typeof(T1))
        {
            if (this.index == 1)
            {
                value = (T)(object)this.field1!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T2))
        {
            if (this.index == 2)
            {
                value = (T)(object)this.field2!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    public override string? ToString() => (this as IOneOf).Value?.ToString();
}

/// <summary>
/// A discriminated union implementation for 3 case(s).
/// </summary>
public readonly struct OneOf<T1, T2, T3> : IOneOf
{
    object? IOneOf.Value => this.index switch
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

    public OneOf(T1 value) : this(1, value, default!, default!)
    {
    }

    public OneOf(T2 value) : this(2, default!, value, default!)
    {
    }

    public OneOf(T3 value) : this(3, default!, default!, value)
    {
    }

    public static implicit operator OneOf<T1, T2, T3>(T1 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3>(T2 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3>(T3 value) => new(value);
    public T As<T>() => this.Is<T>(out var value) ? value : throw new InvalidCastException();
    public bool Is<T>() => this.Is<T>(out _);
    public bool Is<T>([MaybeNullWhen(false)] out T value)
    {
        if (typeof(T) == typeof(T1))
        {
            if (this.index == 1)
            {
                value = (T)(object)this.field1!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T2))
        {
            if (this.index == 2)
            {
                value = (T)(object)this.field2!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T3))
        {
            if (this.index == 3)
            {
                value = (T)(object)this.field3!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    public override string? ToString() => (this as IOneOf).Value?.ToString();
}

/// <summary>
/// A discriminated union implementation for 4 case(s).
/// </summary>
public readonly struct OneOf<T1, T2, T3, T4> : IOneOf
{
    object? IOneOf.Value => this.index switch
    {
        1 => this.field1,
        2 => this.field2,
        3 => this.field3,
        4 => this.field4,
        _ => throw new InvalidOperationException(),
    };
    private readonly byte index;
    private readonly T1 field1;
    private readonly T2 field2;
    private readonly T3 field3;
    private readonly T4 field4;
    private OneOf(byte index, T1 field1, T2 field2, T3 field3, T4 field4)
    {
        this.index = index;
        this.field1 = field1;
        this.field2 = field2;
        this.field3 = field3;
        this.field4 = field4;
    }

    public OneOf(T1 value) : this(1, value, default!, default!, default!)
    {
    }

    public OneOf(T2 value) : this(2, default!, value, default!, default!)
    {
    }

    public OneOf(T3 value) : this(3, default!, default!, value, default!)
    {
    }

    public OneOf(T4 value) : this(4, default!, default!, default!, value)
    {
    }

    public static implicit operator OneOf<T1, T2, T3, T4>(T1 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4>(T2 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4>(T3 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4>(T4 value) => new(value);
    public T As<T>() => this.Is<T>(out var value) ? value : throw new InvalidCastException();
    public bool Is<T>() => this.Is<T>(out _);
    public bool Is<T>([MaybeNullWhen(false)] out T value)
    {
        if (typeof(T) == typeof(T1))
        {
            if (this.index == 1)
            {
                value = (T)(object)this.field1!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T2))
        {
            if (this.index == 2)
            {
                value = (T)(object)this.field2!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T3))
        {
            if (this.index == 3)
            {
                value = (T)(object)this.field3!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T4))
        {
            if (this.index == 4)
            {
                value = (T)(object)this.field4!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    public override string? ToString() => (this as IOneOf).Value?.ToString();
}

/// <summary>
/// A discriminated union implementation for 5 case(s).
/// </summary>
public readonly struct OneOf<T1, T2, T3, T4, T5> : IOneOf
{
    object? IOneOf.Value => this.index switch
    {
        1 => this.field1,
        2 => this.field2,
        3 => this.field3,
        4 => this.field4,
        5 => this.field5,
        _ => throw new InvalidOperationException(),
    };
    private readonly byte index;
    private readonly T1 field1;
    private readonly T2 field2;
    private readonly T3 field3;
    private readonly T4 field4;
    private readonly T5 field5;
    private OneOf(byte index, T1 field1, T2 field2, T3 field3, T4 field4, T5 field5)
    {
        this.index = index;
        this.field1 = field1;
        this.field2 = field2;
        this.field3 = field3;
        this.field4 = field4;
        this.field5 = field5;
    }

    public OneOf(T1 value) : this(1, value, default!, default!, default!, default!)
    {
    }

    public OneOf(T2 value) : this(2, default!, value, default!, default!, default!)
    {
    }

    public OneOf(T3 value) : this(3, default!, default!, value, default!, default!)
    {
    }

    public OneOf(T4 value) : this(4, default!, default!, default!, value, default!)
    {
    }

    public OneOf(T5 value) : this(5, default!, default!, default!, default!, value)
    {
    }

    public static implicit operator OneOf<T1, T2, T3, T4, T5>(T1 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5>(T2 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5>(T3 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5>(T4 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5>(T5 value) => new(value);
    public T As<T>() => this.Is<T>(out var value) ? value : throw new InvalidCastException();
    public bool Is<T>() => this.Is<T>(out _);
    public bool Is<T>([MaybeNullWhen(false)] out T value)
    {
        if (typeof(T) == typeof(T1))
        {
            if (this.index == 1)
            {
                value = (T)(object)this.field1!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T2))
        {
            if (this.index == 2)
            {
                value = (T)(object)this.field2!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T3))
        {
            if (this.index == 3)
            {
                value = (T)(object)this.field3!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T4))
        {
            if (this.index == 4)
            {
                value = (T)(object)this.field4!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T5))
        {
            if (this.index == 5)
            {
                value = (T)(object)this.field5!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    public override string? ToString() => (this as IOneOf).Value?.ToString();
}

/// <summary>
/// A discriminated union implementation for 6 case(s).
/// </summary>
public readonly struct OneOf<T1, T2, T3, T4, T5, T6> : IOneOf
{
    object? IOneOf.Value => this.index switch
    {
        1 => this.field1,
        2 => this.field2,
        3 => this.field3,
        4 => this.field4,
        5 => this.field5,
        6 => this.field6,
        _ => throw new InvalidOperationException(),
    };
    private readonly byte index;
    private readonly T1 field1;
    private readonly T2 field2;
    private readonly T3 field3;
    private readonly T4 field4;
    private readonly T5 field5;
    private readonly T6 field6;
    private OneOf(byte index, T1 field1, T2 field2, T3 field3, T4 field4, T5 field5, T6 field6)
    {
        this.index = index;
        this.field1 = field1;
        this.field2 = field2;
        this.field3 = field3;
        this.field4 = field4;
        this.field5 = field5;
        this.field6 = field6;
    }

    public OneOf(T1 value) : this(1, value, default!, default!, default!, default!, default!)
    {
    }

    public OneOf(T2 value) : this(2, default!, value, default!, default!, default!, default!)
    {
    }

    public OneOf(T3 value) : this(3, default!, default!, value, default!, default!, default!)
    {
    }

    public OneOf(T4 value) : this(4, default!, default!, default!, value, default!, default!)
    {
    }

    public OneOf(T5 value) : this(5, default!, default!, default!, default!, value, default!)
    {
    }

    public OneOf(T6 value) : this(6, default!, default!, default!, default!, default!, value)
    {
    }

    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6>(T1 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6>(T2 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6>(T3 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6>(T4 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6>(T5 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6>(T6 value) => new(value);
    public T As<T>() => this.Is<T>(out var value) ? value : throw new InvalidCastException();
    public bool Is<T>() => this.Is<T>(out _);
    public bool Is<T>([MaybeNullWhen(false)] out T value)
    {
        if (typeof(T) == typeof(T1))
        {
            if (this.index == 1)
            {
                value = (T)(object)this.field1!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T2))
        {
            if (this.index == 2)
            {
                value = (T)(object)this.field2!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T3))
        {
            if (this.index == 3)
            {
                value = (T)(object)this.field3!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T4))
        {
            if (this.index == 4)
            {
                value = (T)(object)this.field4!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T5))
        {
            if (this.index == 5)
            {
                value = (T)(object)this.field5!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T6))
        {
            if (this.index == 6)
            {
                value = (T)(object)this.field6!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    public override string? ToString() => (this as IOneOf).Value?.ToString();
}

/// <summary>
/// A discriminated union implementation for 7 case(s).
/// </summary>
public readonly struct OneOf<T1, T2, T3, T4, T5, T6, T7> : IOneOf
{
    object? IOneOf.Value => this.index switch
    {
        1 => this.field1,
        2 => this.field2,
        3 => this.field3,
        4 => this.field4,
        5 => this.field5,
        6 => this.field6,
        7 => this.field7,
        _ => throw new InvalidOperationException(),
    };
    private readonly byte index;
    private readonly T1 field1;
    private readonly T2 field2;
    private readonly T3 field3;
    private readonly T4 field4;
    private readonly T5 field5;
    private readonly T6 field6;
    private readonly T7 field7;
    private OneOf(byte index, T1 field1, T2 field2, T3 field3, T4 field4, T5 field5, T6 field6, T7 field7)
    {
        this.index = index;
        this.field1 = field1;
        this.field2 = field2;
        this.field3 = field3;
        this.field4 = field4;
        this.field5 = field5;
        this.field6 = field6;
        this.field7 = field7;
    }

    public OneOf(T1 value) : this(1, value, default!, default!, default!, default!, default!, default!)
    {
    }

    public OneOf(T2 value) : this(2, default!, value, default!, default!, default!, default!, default!)
    {
    }

    public OneOf(T3 value) : this(3, default!, default!, value, default!, default!, default!, default!)
    {
    }

    public OneOf(T4 value) : this(4, default!, default!, default!, value, default!, default!, default!)
    {
    }

    public OneOf(T5 value) : this(5, default!, default!, default!, default!, value, default!, default!)
    {
    }

    public OneOf(T6 value) : this(6, default!, default!, default!, default!, default!, value, default!)
    {
    }

    public OneOf(T7 value) : this(7, default!, default!, default!, default!, default!, default!, value)
    {
    }

    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7>(T1 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7>(T2 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7>(T3 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7>(T4 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7>(T5 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7>(T6 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7>(T7 value) => new(value);
    public T As<T>() => this.Is<T>(out var value) ? value : throw new InvalidCastException();
    public bool Is<T>() => this.Is<T>(out _);
    public bool Is<T>([MaybeNullWhen(false)] out T value)
    {
        if (typeof(T) == typeof(T1))
        {
            if (this.index == 1)
            {
                value = (T)(object)this.field1!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T2))
        {
            if (this.index == 2)
            {
                value = (T)(object)this.field2!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T3))
        {
            if (this.index == 3)
            {
                value = (T)(object)this.field3!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T4))
        {
            if (this.index == 4)
            {
                value = (T)(object)this.field4!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T5))
        {
            if (this.index == 5)
            {
                value = (T)(object)this.field5!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T6))
        {
            if (this.index == 6)
            {
                value = (T)(object)this.field6!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T7))
        {
            if (this.index == 7)
            {
                value = (T)(object)this.field7!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    public override string? ToString() => (this as IOneOf).Value?.ToString();
}

/// <summary>
/// A discriminated union implementation for 8 case(s).
/// </summary>
public readonly struct OneOf<T1, T2, T3, T4, T5, T6, T7, T8> : IOneOf
{
    object? IOneOf.Value => this.index switch
    {
        1 => this.field1,
        2 => this.field2,
        3 => this.field3,
        4 => this.field4,
        5 => this.field5,
        6 => this.field6,
        7 => this.field7,
        8 => this.field8,
        _ => throw new InvalidOperationException(),
    };
    private readonly byte index;
    private readonly T1 field1;
    private readonly T2 field2;
    private readonly T3 field3;
    private readonly T4 field4;
    private readonly T5 field5;
    private readonly T6 field6;
    private readonly T7 field7;
    private readonly T8 field8;
    private OneOf(byte index, T1 field1, T2 field2, T3 field3, T4 field4, T5 field5, T6 field6, T7 field7, T8 field8)
    {
        this.index = index;
        this.field1 = field1;
        this.field2 = field2;
        this.field3 = field3;
        this.field4 = field4;
        this.field5 = field5;
        this.field6 = field6;
        this.field7 = field7;
        this.field8 = field8;
    }

    public OneOf(T1 value) : this(1, value, default!, default!, default!, default!, default!, default!, default!)
    {
    }

    public OneOf(T2 value) : this(2, default!, value, default!, default!, default!, default!, default!, default!)
    {
    }

    public OneOf(T3 value) : this(3, default!, default!, value, default!, default!, default!, default!, default!)
    {
    }

    public OneOf(T4 value) : this(4, default!, default!, default!, value, default!, default!, default!, default!)
    {
    }

    public OneOf(T5 value) : this(5, default!, default!, default!, default!, value, default!, default!, default!)
    {
    }

    public OneOf(T6 value) : this(6, default!, default!, default!, default!, default!, value, default!, default!)
    {
    }

    public OneOf(T7 value) : this(7, default!, default!, default!, default!, default!, default!, value, default!)
    {
    }

    public OneOf(T8 value) : this(8, default!, default!, default!, default!, default!, default!, default!, value)
    {
    }

    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7, T8>(T2 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7, T8>(T3 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7, T8>(T4 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7, T8>(T5 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7, T8>(T6 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7, T8>(T7 value) => new(value);
    public static implicit operator OneOf<T1, T2, T3, T4, T5, T6, T7, T8>(T8 value) => new(value);
    public T As<T>() => this.Is<T>(out var value) ? value : throw new InvalidCastException();
    public bool Is<T>() => this.Is<T>(out _);
    public bool Is<T>([MaybeNullWhen(false)] out T value)
    {
        if (typeof(T) == typeof(T1))
        {
            if (this.index == 1)
            {
                value = (T)(object)this.field1!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T2))
        {
            if (this.index == 2)
            {
                value = (T)(object)this.field2!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T3))
        {
            if (this.index == 3)
            {
                value = (T)(object)this.field3!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T4))
        {
            if (this.index == 4)
            {
                value = (T)(object)this.field4!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T5))
        {
            if (this.index == 5)
            {
                value = (T)(object)this.field5!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T6))
        {
            if (this.index == 6)
            {
                value = (T)(object)this.field6!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T7))
        {
            if (this.index == 7)
            {
                value = (T)(object)this.field7!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        if (typeof(T) == typeof(T8))
        {
            if (this.index == 8)
            {
                value = (T)(object)this.field8!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    public override string? ToString() => (this as IOneOf).Value?.ToString();
}
