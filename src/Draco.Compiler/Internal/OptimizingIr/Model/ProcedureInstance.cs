using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents a generic instantiation of a procedure.
/// </summary>
/// <param name="Symbol">The instantiated function symbol.</param>
/// <param name="Procedure">The instantiated procedure.</param>
internal readonly record struct ProcedureInstance(FunctionSymbol Symbol, IOperand Procedure) : IOperand
{
    public TypeSymbol? Type => this.Symbol.Type;

    public ImmutableArray<TypeSymbol> Arguments => this.Symbol.GenericArguments;

    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => $"[{this.Symbol.FullName}]";
}
