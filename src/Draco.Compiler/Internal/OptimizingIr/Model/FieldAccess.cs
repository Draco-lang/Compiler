using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents a field access.
/// </summary>
/// <param name="Receiver">The accessed reciever.</param>
/// <param name="Member">The accessed member.</param>
internal readonly record struct FieldAccess(IOperand Receiver, FieldSymbol Member) : IOperand
{
    public TypeSymbol? Type => this.Member.Type;

    public bool IsStatic => this.Member.IsStatic;

    public override string ToString() => this.ToOperandString();

    public string ToOperandString() => $"{this.Receiver}.{this.Member.Name}";
}
