using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;
internal class Box<T>(T value)
{
    protected T value = value;
    public T Value => this.value;

    public static implicit operator Box<T>(T value) => new(value);
}

internal class MutableBox<T>(T value, bool canSetValue) : Box<T>(value)
{
    public bool CanSetValue { get; } = canSetValue;
    public new T Value
    {
        get => base.Value;
        set
        {
            if (!this.CanSetValue) throw new InvalidOperationException("Cannot set value");
            this.value = value;
        }
    }
}
