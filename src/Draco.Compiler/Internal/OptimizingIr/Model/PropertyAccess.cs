using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

internal readonly record struct PropertyAccess(IOperand Receiver, PropertySymbol Member) : IOperand
{
    public TypeSymbol? Type => this.Member.Type;

    public bool IsStatic => this.Member.IsStatic;

    public override string ToString() => this.ToOperandString();

    public string ToOperandString() => $"{this.Receiver}.{this.Member.Name}";
}
