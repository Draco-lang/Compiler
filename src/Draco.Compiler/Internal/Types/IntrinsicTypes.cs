namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Types known by the compiler.
/// </summary>
internal static class IntrinsicTypes
{
    public static Type Never { get; } = NeverType.Instance;
    public static Type Error { get; } = ErrorType.Instance;
    public static Type Unit { get; } = new BuiltinType(typeof(void), "unit");

    public static Type Int8 { get; } = new BuiltinType(typeof(sbyte), "int8");
    public static Type Int16 { get; } = new BuiltinType(typeof(short), "int16");
    public static Type Int32 { get; } = new BuiltinType(typeof(int), "int32");
    public static Type Int64 { get; } = new BuiltinType(typeof(long), "int64");

    public static Type Uint8 { get; } = new BuiltinType(typeof(byte), "uint8");
    public static Type Uint16 { get; } = new BuiltinType(typeof(ushort), "uint16");
    public static Type Uint32 { get; } = new BuiltinType(typeof(uint), "uint32");
    public static Type Uint64 { get; } = new BuiltinType(typeof(ulong), "uint64");

    public static Type Float32 { get; } = new BuiltinType(typeof(float), "float32");
    public static Type Float64 { get; } = new BuiltinType(typeof(double), "float64");

    public static Type Bool { get; } = new BuiltinType(typeof(bool), "bool");
    public static Type String { get; } = new BuiltinType(typeof(string), "string");
}
