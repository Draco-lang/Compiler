using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Draco.SourceGeneration;

/// <summary>
/// Reimplementation of the BCL type because of ns2.0 target.
/// </summary>
internal sealed class ReferenceEqualityComparer : IEqualityComparer<object?>
{
    public static ReferenceEqualityComparer Instance { get; } = new();

    private ReferenceEqualityComparer()
    {
    }

    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
    public int GetHashCode(object? obj) => RuntimeHelpers.GetHashCode(obj);
}
