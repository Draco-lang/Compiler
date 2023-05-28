using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a lazily discovered array in the debugged program.
/// </summary>
public sealed class ArrayValue : IReadOnlyList<object?>
{
    private struct Slot
    {
        public bool Initialized;
        public object? Value;
    }

    public int Count => this.value.Count;
    public object? this[int index]
    {
        get
        {
            this.slots ??= new Slot[this.Count];
            var slot = this.slots[index];
            if (!slot.Initialized)
            {
                slot.Initialized = true;
                slot.Value = this.value.GetElementAtPosition(index).ToBrowsableObject();
                this.slots[index] = slot;
            }
            return slot;
        }
    }

    private readonly CorDebugArrayValue value;
    private Slot[]? slots;

    internal ArrayValue(CorDebugArrayValue value)
    {
        this.value = value;
    }

    public override string ToString() => $"[{this.Count}]{{{string.Join(", ", this)}}}";

    public IEnumerator<object?> GetEnumerator() => Enumerable
        .Range(0, this.Count)
        .Select(i => this[i])
        .GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
