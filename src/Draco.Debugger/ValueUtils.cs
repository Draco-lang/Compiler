using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
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
        case CorElementType.Boolean:
            return value.GetIntegralValue() != 0;

        case CorElementType.Char:
            return (char)value.GetIntegralValue();

        case CorElementType.I:
            return value.GetIntegralValue();
        case CorElementType.I1:
            return (sbyte)value.GetIntegralValue();
        case CorElementType.I2:
            return (short)value.GetIntegralValue();
        case CorElementType.I4:
            return (int)value.GetIntegralValue();
        case CorElementType.I8:
            return (long)value.GetIntegralValue();

        case CorElementType.U:
            return (nuint)value.GetIntegralValue();
        case CorElementType.U1:
            return (byte)value.GetIntegralValue();
        case CorElementType.U2:
            return (ushort)value.GetIntegralValue();
        case CorElementType.U4:
            return (uint)value.GetIntegralValue();
        case CorElementType.U8:
            return (ulong)value.GetIntegralValue();

        case CorElementType.R4:
        {
            var bytes = value.GetIntegralValue();
            return BitConverter.ToSingle(BitConverter.GetBytes(bytes));
        }
        case CorElementType.R8:
        {
            var bytes = value.GetIntegralValue();
            return BitConverter.ToDouble(BitConverter.GetBytes(bytes));
        }

        case CorElementType.String:
        {
            // NOTE: I have no idea why, but this is the only reliable way I can read out the string
            var strValue = new CorDebugStringValue((ICorDebugStringValue)value.Raw);
            var len = strValue.Length;
            var sb = new StringBuilder(len);
            var result = strValue.Raw.GetString(len, out _, sb);
            if (result != HRESULT.S_OK) throw new InvalidOperationException("failed to read out string");
            return sb.ToString();
        }

        case CorElementType.SZArray:
        {
            // NOTE: I have no idea why, but this is the only reliable way I can reach the array value type
            var arrayValue = new CorDebugArrayValue((ICorDebugArrayValue)value.Raw);
            return new ArrayValue(arrayValue);
        }

        case CorElementType.Class:
        {
            // NOTE: I have no idea why, but this is the only reliable way I can reach the class value type
            var objectValue = (ICorDebugObjectValue)value.Raw;
            return new ObjectValue(objectValue);
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
