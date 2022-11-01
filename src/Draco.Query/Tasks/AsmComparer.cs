using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query.Tasks;

/// <summary>
/// Compares two <see cref="IAsyncStateMachine"/>s for value-equality.
/// </summary>
internal sealed class AsmComparer : IEqualityComparer<IAsyncStateMachine>
{
    /// <summary>
    /// A singleton instance of this comparer.
    /// </summary>
    public static AsmComparer Instance { get; } = new();

    private AsmComparer()
    {
    }

    public bool Equals(IAsyncStateMachine? x, IAsyncStateMachine? y) =>
        AsmComparerCache.Equals(x, y);

    public int GetHashCode([DisallowNull] IAsyncStateMachine obj) =>
        AsmComparerCache.GetHashCode(obj);
}

/// <summary>
/// Implements the actual comparisons for async state machines with compiled expression trees.
/// </summary>
internal static class AsmComparerCache
{
    private delegate bool AsmEqualsDelegate(IAsyncStateMachine x, IAsyncStateMachine y);
    private delegate int AsmGetHashCodeDelegate(IAsyncStateMachine obj);

    private static readonly ConcurrentDictionary<Type, AsmEqualsDelegate> equalsInstances = new();
    private static readonly ConcurrentDictionary<Type, AsmGetHashCodeDelegate> hashCodeInstances = new();

    public static bool Equals(IAsyncStateMachine? x, IAsyncStateMachine? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        var t1 = x.GetType();
        var t2 = y.GetType();
        if (t1 != t2) return false;
        var equals = equalsInstances.GetOrAdd(t1, CreateEqualsFunc);
        return equals(x, y);
    }

    public static int GetHashCode([DisallowNull] IAsyncStateMachine obj)
    {
        var type = obj.GetType();
        var hashCode = hashCodeInstances.GetOrAdd(type, CreateHashCodeFunc);
        return hashCode(obj);
    }

    private static AsmEqualsDelegate CreateEqualsFunc(Type asmType)
    {
        var getTypeMethod = asmType.GetMethod(nameof(GetType))!;

        var param1 = Expression.Parameter(typeof(IAsyncStateMachine));
        var param2 = Expression.Parameter(typeof(IAsyncStateMachine));

        var param1AsConcreteType = Expression.Variable(asmType);
        var param2AsConcreteType = Expression.Variable(asmType);

        var blockExprs = new List<Expression>();
        blockExprs.Add(Expression.Assign(param1AsConcreteType, CastToConcreteType(param1, asmType)));
        blockExprs.Add(Expression.Assign(param2AsConcreteType, CastToConcreteType(param2, asmType)));

        var comparisons = GetRelevantFields(asmType)
            .Select(f => Expression.Equal(
                Expression.MakeMemberAccess(param1AsConcreteType, f),
                Expression.MakeMemberAccess(param2AsConcreteType, f)));
        var comparisonsConjuncted = comparisons
            .Cast<Expression>()
            .Prepend(Expression.Constant(true))
            .Aggregate(Expression.AndAlso);
        blockExprs.Add(comparisonsConjuncted);

        var block = Expression.Block(
            variables: new[] { param1AsConcreteType, param2AsConcreteType },
            expressions: blockExprs);
        var lambda = Expression.Lambda<Func<IAsyncStateMachine, IAsyncStateMachine, bool>>(
            block,
            new[] { param1, param2 });

        return new(lambda.Compile());
    }

    private static AsmGetHashCodeDelegate CreateHashCodeFunc(Type asmType)
    {
        var getTypeMethod = asmType.GetMethod(nameof(GetType))!;
        var toHashCodeMethod = typeof(HashCode).GetMethod(nameof(HashCode.ToHashCode))!;

        var param = Expression.Parameter(typeof(IAsyncStateMachine));

        var hashCode = Expression.Variable(typeof(HashCode));
        var asmAsConcreteType = Expression.Variable(asmType);

        var blockExprs = new List<Expression>();
        blockExprs.Add(Expression.Assign(hashCode, Expression.Default(typeof(HashCode))));
        blockExprs.Add(Expression.Assign(asmAsConcreteType, CastToConcreteType(param, asmType)));
        blockExprs.Add(Expression.Call(
            instance: hashCode,
            methodName: nameof(HashCode.Add),
            typeArguments: new[] { typeof(Type) },
            arguments: Expression.Constant(asmType)));

        foreach (var field in GetRelevantFields(asmType))
            blockExprs.Add(Expression.Call(
                instance: hashCode,
                methodName: nameof(HashCode.Add),
                typeArguments: new[] { field.FieldType },
                arguments: Expression.MakeMemberAccess(asmAsConcreteType, field)));

        blockExprs.Add(Expression.Call(
            instance: hashCode,
            method: toHashCodeMethod));

        var block = Expression.Block(
            variables: new[] { hashCode, asmAsConcreteType },
            expressions: blockExprs);
        var lambda = Expression.Lambda<Func<IAsyncStateMachine, int>>(block, param);

        return new(lambda.Compile());
    }

    private static Expression CastToConcreteType(Expression expr, Type asmType) => asmType.IsValueType
        ? Expression.Convert(expr, asmType)
        : Expression.Call(
              type: typeof(Unsafe),
              methodName: nameof(Unsafe.As),
              typeArguments: new[] { asmType },
              arguments: expr);

    private static IEnumerable<FieldInfo> GetRelevantFields(Type asmType) => asmType
        .GetFields()
        .Where(f => !f.Name.Contains('<'));
}
