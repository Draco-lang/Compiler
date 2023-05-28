using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Utilities for handling <see cref="CorDebugValue"/>s.
/// </summary>
internal static class ValueUtils
{
    /// <summary>
    /// Converts the given <see cref="CorDebugValue"/> to a browsable C# object representation.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted <paramref name="value"/>.</returns>
    public static object? ToBrowsableObject(this CorDebugValue value)
    {
        value = value.DereferenceAndUnbox(out var isNull);
        if (isNull) return null;

        switch (value.Type)
        {
        case CorElementType.I4:
            return (int)value.GetIntegralValue();

        case CorElementType.String:
        {
            // NOTE: I have no idea why, but this is the only reliable way I cn read out the string
            var strValue = new CorDebugStringValue((ICorDebugStringValue)value.Raw);
            var len = strValue.Length;
            var sb = new StringBuilder(len);
            var result = strValue.Raw.GetString(len, out _, sb);
            if (result != HRESULT.S_OK) throw new InvalidOperationException("failed to read out string");
            return sb.ToString();
        }

        default:
            throw new ArgumentOutOfRangeException(nameof(value));
        }
    }

    private static nint GetIntegralValue(this CorDebugValue value)
    {
        if (value is not CorDebugGenericValue genericValue)
        {
            throw new ArgumentException("the provided value can not provide a generic view", nameof(value));
        }
        return genericValue.Value;
    }

    private static CorDebugValue DereferenceAndUnbox(this CorDebugValue value, out bool isNull)
    {
        while (true)
        {
            if (value is CorDebugReferenceValue refValue)
            {
                if (refValue.IsNull)
                {
                    isNull = true;
                    return value;
                }
                value = refValue.Dereference();
                continue;
            }
            if (value is CorDebugBoxValue boxValue)
            {
                value = boxValue.Object;
                continue;
            }
            break;
        }
        isNull = false;
        return value;
    }
}
