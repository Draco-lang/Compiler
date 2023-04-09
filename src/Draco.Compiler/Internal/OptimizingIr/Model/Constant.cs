using Draco.Compiler.Internal.Types;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A constant value.
/// </summary>
/// <param name="Value">The constant value.</param>
internal readonly record struct Constant(object? Value, Type Type) : IOperand
{
    // TODO: probably get type from IR or Binder, this is not good
    //public Type? Type => this.Value switch
    //{
    //    int => IntrinsicTypes.Int32,
    //    bool => IntrinsicTypes.Bool,
    //    string => IntrinsicTypes.String,
    //    _ => throw new System.InvalidOperationException(),
    //};

    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => this.Value switch
    {
        string s => $"\"{StringUtils.Unescape(s)}\"",
        bool b => b ? "true" : "false",
        _ => this.Value?.ToString() ?? "null",
    };
}
