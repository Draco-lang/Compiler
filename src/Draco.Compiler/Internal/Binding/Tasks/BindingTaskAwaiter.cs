using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding.Tasks;

internal struct BindingTaskAwaiter<T> : INotifyCompletion, IBindingTaskAwaiter
{
    public bool IsCompleted { get; private set; }
    public ConstraintSolver Solver { get; set; }

    public TypeSymbol? ResultType
    {
        get
        {
            if (this.result is not null) return ExtractType(this.result);

            this.allocatedResultType ??= this.Solver.AllocateTypeVariable();
            return this.allocatedResultType;
        }
    }

    private T? result;
    private Exception? exception;
    private List<Action>? completions;
    private TypeSymbol? allocatedResultType;

    internal void SetResult(T? result, Exception? exception)
    {
        this.IsCompleted = true;
        this.result = result;
        this.exception = exception;
        if (this.allocatedResultType is not null)
        {
            var type = ExtractType(result!);
            this.Solver.UnifyAsserted(this.allocatedResultType, type!);
        }
        foreach (var completion in this.completions ?? Enumerable.Empty<Action>())
        {
            completion();
        }
    }

    public readonly T GetResult()
    {
        if (this.exception is not null) throw this.exception;
        return this.result!;
    }

    public void OnCompleted(Action completion)
    {
        if (this.IsCompleted)
        {
            completion();
        }
        else
        {
            this.completions ??= new();
            this.completions.Add(completion);
        }
    }

    private static TypeSymbol? ExtractType(T value) => value switch
    {
        BoundStatement => null,
        BoundExpression expr => expr.Type,
        BoundLvalue lvalue => lvalue.Type,
        _ => throw new InvalidOperationException(),
    };
}
