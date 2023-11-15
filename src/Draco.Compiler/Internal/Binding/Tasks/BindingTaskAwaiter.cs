using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Binding.Tasks;

internal sealed class BindingTaskAwaiter<T> : INotifyCompletion
{
    public bool IsCompleted { get; private set; }

    private T? result;
    private Exception? exception;
    private List<Action>? completions;
    private TypeSymbol? resultType;

    public TypeSymbol GetResultType(SyntaxNode? syntax, ConstraintSolver solver, DiagnosticBag diagnostics)
    {
        if (this.result is not null)
        {
            var type = ExtractType(this.result);
            if (type is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    template: TypeCheckingErrors.IllegalExpression,
                    location: syntax?.Location));
                type = IntrinsicSymbols.ErrorType;
            }
            return type;
        }

        this.resultType ??= solver.AllocateTypeVariable();
        return this.resultType;
    }

    internal void SetResult(T? result)
    {
        this.IsCompleted = true;
        this.result = result;
        if (this.resultType is not null)
        {
            var type = ExtractType(result!);
            ConstraintSolver.UnifyAsserted(this.resultType, type!);
        }
        this.RunCompletions();
    }

    internal void SetException(Exception? exception)
    {
        this.IsCompleted = true;
        this.exception = exception;
        this.RunCompletions();
    }

    public T GetResult()
    {
        if (this.exception is not null)
        {
            if (this.exception is not AggregateException) this.exception = new AggregateException(this.exception);
            throw this.exception;
        }
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

    private void RunCompletions()
    {
        foreach (var completion in this.completions ?? Enumerable.Empty<Action>())
        {
            completion();
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
