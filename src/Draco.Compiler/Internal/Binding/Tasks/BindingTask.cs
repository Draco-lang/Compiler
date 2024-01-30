using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding.Tasks;

internal static class BindingTask
{
    public static BindingTask<T> FromResult<T>(T result)
    {
        var task = new BindingTask<T>();
        task.Awaiter.SetResult(result);
        return task;
    }

    public static async SolverTask<ImmutableArray<T>> WhenAll<T>(IEnumerable<BindingTask<T>> tasks)
    {
        var result = ImmutableArray.CreateBuilder<T>();
        foreach (var task in tasks) result.Add(await task);
        return result.ToImmutable();
    }
}

[AsyncMethodBuilder(typeof(BindingTaskMethodBuilder<>))]
internal sealed class BindingTask<T>
{
    internal BindingTaskAwaiter<T> Awaiter = new();
    public bool IsCompleted => this.Awaiter.IsCompleted;
    public T Result => this.Awaiter.GetResult();
    public BindingTaskAwaiter<T> GetAwaiter() => this.Awaiter;
    public TypeSymbol GetResultType(SyntaxNode? syntax, ConstraintSolver solver, DiagnosticBag diagnostics) =>
        this.Awaiter.GetResultType(syntax, solver, diagnostics);
}
