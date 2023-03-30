namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Types known by the compiler.
/// </summary>
internal static class IntrinsicTypes
{
    public static Type Never { get; } = NeverType.Instance;
    public static Type Error { get; } = ErrorType.Instance;
    public static Type Unit { get; } = new BuiltinType(typeof(void), "unit");
    public static Type Int32 { get; } = new BuiltinType(typeof(int), "int32");
    public static Type Float64 { get; } = new BuiltinType(typeof(double), "float64");
    public static Type Bool { get; } = new BuiltinType(typeof(bool), "bool");
    public static Type String { get; } = new BuiltinType(typeof(string), "string");
}
