using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query.Tasks;

/// <summary>
/// Utilities for async state machines.
/// </summary>
internal static class AsmUtils
{
    private static readonly ConcurrentDictionary<Type, AsmCodegen.EqualsDelegate> equalsInstances = new();
    private static readonly ConcurrentDictionary<Type, AsmCodegen.GetHashCodeDelegate> getHashCodeInstances = new();
    private static readonly ConcurrentDictionary<Type, Delegate> cloneInstances = new();
    private static readonly ConcurrentDictionary<Type, Delegate> getBuilderInstances = new();
    private static readonly ConcurrentDictionary<Type, Delegate> getQueryDatabaseInstances = new();

    public static bool Equals(IAsyncStateMachine? x, IAsyncStateMachine? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        var t1 = x.GetType();
        var t2 = y.GetType();
        if (t1 != t2) return false;
        var equals = equalsInstances.GetOrAdd(t1, AsmCodegen.GenerateEquals);
        return equals(x, y);
    }

    public static int GetHashCode([DisallowNull] IAsyncStateMachine obj)
    {
        var type = obj.GetType();
        var hashCode = getHashCodeInstances.GetOrAdd(type, AsmCodegen.GenerateGetHashCode);
        return hashCode(obj);
    }

    public static TAsm Clone<TAsm>(ref TAsm obj)
        where TAsm : IAsyncStateMachine
    {
        var type = obj.GetType();
        var cloneDelegate = cloneInstances.GetOrAdd(type, AsmCodegen.GenerateClone<TAsm>);
        var clone = Unsafe.As<AsmCodegen.CloneDelegate<TAsm>>(cloneDelegate);
        return clone(ref obj);
    }

    public static ref TBuilder GetBuilder<TAsm, TBuilder>(ref TAsm obj)
        where TAsm : IAsyncStateMachine
    {
        var type = obj.GetType();
        var getBuilderDelegate = getBuilderInstances.GetOrAdd(type, AsmCodegen.GenerateGetBuilder<TAsm, TBuilder>);
        var getBuilder = Unsafe.As<AsmCodegen.GetBuilderDelegate<TAsm, TBuilder>>(getBuilderDelegate);
        return ref getBuilder(ref obj);
    }

    public static QueryDatabase GetQueryDatabase<TAsm>(ref TAsm obj)
        where TAsm : IAsyncStateMachine
    {
        var type = obj.GetType();
        var getQueryDatabaseDelegate = getQueryDatabaseInstances.GetOrAdd(type, AsmCodegen.GenerateGetQueryDatabase<TAsm>);
        var getQueryDatabase = Unsafe.As<AsmCodegen.GetQueryDatabaseDelegate<TAsm>>(getQueryDatabaseDelegate);
        return getQueryDatabase(ref obj);
    }
}

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
        AsmUtils.Equals(x, y);

    public int GetHashCode([DisallowNull] IAsyncStateMachine obj) =>
        AsmUtils.GetHashCode(obj);
}

/// <summary>
/// Generates async state machine manipulation code at runtime.
/// </summary>
internal static class AsmCodegen
{
    public delegate bool EqualsDelegate(IAsyncStateMachine x, IAsyncStateMachine y);
    public delegate int GetHashCodeDelegate(IAsyncStateMachine obj);
    public delegate TAsm CloneDelegate<TAsm>(ref TAsm obj);
    public delegate ref TBuilder GetBuilderDelegate<TAsm, TBuilder>(ref TAsm obj);
    public delegate QueryDatabase GetQueryDatabaseDelegate<TAsm>(ref TAsm obj);

    private static readonly MethodInfo getTypeMethod;
    private static readonly MethodInfo toHashCodeMethod;
    private static readonly MethodInfo memberwiseCloneMethod;

    static AsmCodegen()
    {
        getTypeMethod = typeof(object).GetMethod(nameof(GetType))!;
        toHashCodeMethod = typeof(HashCode).GetMethod(nameof(HashCode.ToHashCode))!;
        memberwiseCloneMethod = typeof(object).GetMethod(
            nameof(MemberwiseClone),
            BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    public static EqualsDelegate GenerateEquals(Type asmType)
    {
        var param1 = Expression.Parameter(typeof(IAsyncStateMachine));
        var param2 = Expression.Parameter(typeof(IAsyncStateMachine));

        var param1Casted = Expression.Variable(asmType);
        var param2Casted = Expression.Variable(asmType);

        var blockExprs = new List<Expression>()
        {
            // Optional cast param1 to exact type
            Expression.Assign(param1Casted, GenerateCastAsmToExactType(param1, asmType)),
            // Optional cast param2 to exact type
            Expression.Assign(param2Casted, GenerateCastAsmToExactType(param2, asmType)),
        };

        // Generate each individual comparison between fields
        var comparisons = GetNonSpecialFields(asmType)
            .Select(f => Expression.Equal(
                Expression.Field(param1Casted, f),
                Expression.Field(param2Casted, f)));
        // And them together, add a constant true at the start to make 0-field ones correct code
        var comparisonsConjuncted = comparisons
           .Cast<Expression>()
           .Aggregate(Expression.Constant(true) as Expression, Expression.AndAlso);
        blockExprs.Add(comparisonsConjuncted);

        // Build up result
        var block = Expression.Block(
            variables: new[] { param1Casted, param2Casted },
            expressions: blockExprs);
        var lambda = Expression.Lambda<EqualsDelegate>(block, new[] { param1, param2 });

        // Compile
        return lambda.Compile();
    }

    public static GetHashCodeDelegate GenerateGetHashCode(Type asmType)
    {
        var param = Expression.Parameter(typeof(IAsyncStateMachine));

        var hashCode = Expression.Variable(typeof(HashCode));
        var paramCasted = Expression.Variable(asmType);

        var blockExprs = new List<Expression>()
        {
            // var hashCode = default(HashCode)
            Expression.Assign(hashCode, Expression.Default(typeof(HashCode))),
            // Optional cast to exact type
            Expression.Assign(paramCasted, GenerateCastAsmToExactType(param, asmType)),
            // hashCode.Add(typeof(asm))
            Expression.Call(
                instance: hashCode,
                methodName: nameof(HashCode.Add),
                typeArguments: new[] { typeof(Type) },
                arguments: Expression.Constant(asmType))
        };

        foreach (var field in GetNonSpecialFields(asmType))
        {
            // hashCode.Add(asmCasted.Field)
            blockExprs.Add(Expression.Call(
                instance: hashCode,
                methodName: nameof(HashCode.Add),
                typeArguments: new[] { field.FieldType },
                arguments: Expression.Field(paramCasted, field)));
        }

        // hashCode.ToHashCode()
        blockExprs.Add(Expression.Call(
            instance: hashCode,
            method: toHashCodeMethod));

        // Build up result
        var block = Expression.Block(
            variables: new[] { hashCode, paramCasted },
            expressions: blockExprs);
        var lambda = Expression.Lambda<GetHashCodeDelegate>(block, param);

        // Compile
        return lambda.Compile();
    }

    public static CloneDelegate<TAsm> GenerateClone<TAsm>()
        where TAsm : IAsyncStateMachine
    {
        var asmType = typeof(TAsm);

        var param = Expression.Parameter(asmType.MakeByRefType());
        var paramCasted = GenerateCastAsmToExactType(param, asmType);

        var result = paramCasted;
        if (!asmType.IsValueType)
        {
            // For ref types we need to invoke memberwise clone
            result = Expression.Convert(
                Expression.Call(result, memberwiseCloneMethod),
                asmType);
        }

        // Build up result
        var lambda = Expression.Lambda<CloneDelegate<TAsm>>(result, param);

        // Compile
        return lambda.Compile();
    }

    public static GetBuilderDelegate<TAsm, TBuilder> GenerateGetBuilder<TAsm, TBuilder>()
        where TAsm : IAsyncStateMachine
    {
        var asmType = typeof(TAsm);

        // Extract the field info
        var builderField = asmType.GetField("<>t__builder")!;

        if (builderField.FieldType != typeof(TBuilder))
        {
            throw new InvalidOperationException("Builder field does not match the expected builder type");
        }

        // Since the Expression trees API can't return field refs, we are building the IL ourselves
        var method = new DynamicMethod(
            name: string.Empty,
            returnType: builderField.FieldType.MakeByRefType(),
            parameterTypes: new[] { asmType.MakeByRefType() });

        var emitter = method.GetILGenerator();
        emitter.Emit(OpCodes.Ldarg_0);
        if (!asmType.IsValueType) emitter.Emit(OpCodes.Ldind_Ref);
        emitter.Emit(OpCodes.Ldflda, builderField);
        emitter.Emit(OpCodes.Ret);

        return method.CreateDelegate<GetBuilderDelegate<TAsm, TBuilder>>();
    }

    public static GetQueryDatabaseDelegate<TAsm> GenerateGetQueryDatabase<TAsm>()
        where TAsm : IAsyncStateMachine
    {
        var asmType = typeof(TAsm);

        // Extract the field info
        var queryDbField = asmType
            .GetFields()
            .FirstOrDefault(field => field.FieldType == typeof(QueryDatabase));

        if (queryDbField is null)
        {
            throw new InvalidOperationException("There is no query database taken as a parameter");
        }

        var param = Expression.Parameter(asmType.MakeByRefType());

        var body = Expression.Field(param, queryDbField);

        // Build up result
        var lambda = Expression.Lambda<GetQueryDatabaseDelegate<TAsm>>(body, param);

        // Compile
        return lambda.Compile();
    }

    private static Expression GenerateCastAsmToExactType(Expression asm, Type asmType) => asmType.IsValueType
        // We need to cast, Unsafe.As for ref types
        ? Expression.Convert(asm, asmType)
        : Expression.Call(
              type: typeof(Unsafe),
              methodName: nameof(Unsafe.As),
              typeArguments: new[] { asmType },
              arguments: asm);

    private static IEnumerable<FieldInfo> GetNonSpecialFields(Type asmType) => asmType
        .GetFields()
        .Where(f => !f.Name.Contains('<'));
}
