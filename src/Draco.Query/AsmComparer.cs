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

namespace Draco.Query;

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

        var unsafeAsParam1 = CastToConcreteType(param1, asmType);
        var unsafeAsParam2 = CastToConcreteType(param2, asmType);

        var comparisons = GetRelevantFields(asmType)
            .Select(f => Expression.Equal(
                Expression.MakeMemberAccess(unsafeAsParam1, f),
                Expression.MakeMemberAccess(unsafeAsParam2, f)));
        var comparisonsConjuncted = comparisons
            .Cast<Expression>()
            .Prepend(Expression.Constant(true))
            .Aggregate(Expression.AndAlso);

        var lambda = Expression.Lambda(comparisonsConjuncted, new[] { param1, param2 });

        return new((Func<IAsyncStateMachine, IAsyncStateMachine, bool>)lambda.Compile());
    }

    private static AsmGetHashCodeDelegate CreateHashCodeFunc(Type asmType)
    {
        var getTypeMethod = asmType.GetMethod(nameof(GetType))!;

        var param = Expression.Parameter(typeof(IAsyncStateMachine));

        var hashCombineArgs = new List<Expression>();
        var hashCombineTypeArgs = new List<Type>();

        hashCombineArgs.Add(Expression.Call(param, getTypeMethod));
        hashCombineTypeArgs.Add(typeof(Type));

        var asmAsConcreteType = CastToConcreteType(param, asmType);
        foreach (var field in GetRelevantFields(asmType))
        {
            hashCombineArgs.Add(Expression.MakeMemberAccess(asmAsConcreteType, field));
            hashCombineTypeArgs.Add(field.FieldType);
        }

        var hashCombineCall = Expression.Call(
            type: typeof(HashCode),
            methodName: nameof(HashCode.Combine),
            typeArguments: hashCombineTypeArgs.ToArray(),
            arguments: hashCombineArgs.ToArray());

        var lambda = Expression.Lambda(hashCombineCall, param);

        return new((Func<IAsyncStateMachine, int>)lambda.Compile());
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
