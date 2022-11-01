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
/// Utilities for async state machines.
/// </summary>
internal static class AsmUtils<TAsm, TBuilder>
    where TAsm : IAsyncStateMachine
{
    private static readonly ConcurrentDictionary<Type, AsmCodegen<TAsm, TBuilder>.AsmEqualsDelegate> equalsInstances = new();
    private static readonly ConcurrentDictionary<Type, AsmCodegen<TAsm, TBuilder>.AsmGetHashCodeDelegate> getHashCodeInstances = new();
    private static readonly ConcurrentDictionary<Type, AsmCodegen<TAsm, TBuilder>.AsmGetBuilderDelegate> getBuilderInstances = new();
    private static readonly ConcurrentDictionary<Type, AsmCodegen<TAsm, TBuilder>.AsmSetBuilderDelegate> setBuilderInstances = new();
    private static readonly ConcurrentDictionary<Type, AsmCodegen<TAsm, TBuilder>.AsmCloneDelegate> cloneInstances = new();

    public static bool Equals(TAsm? x, TAsm? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        var t1 = x.GetType();
        var t2 = y.GetType();
        if (t1 != t2) return false;
        var equals = equalsInstances.GetOrAdd(t1, AsmCodegen<TAsm, TBuilder>.GenerateEquals);
        return equals(x, y);
    }

    public static int GetHashCode([DisallowNull] TAsm obj)
    {
        var type = obj.GetType();
        var hashCode = getHashCodeInstances.GetOrAdd(type, AsmCodegen<TAsm, TBuilder>.GenerateGetHashCode);
        return hashCode(obj);
    }

    public static ref TBuilder GetBuilder(ref TAsm obj)
    {
        var type = obj.GetType();
        var getBuilder = getBuilderInstances.GetOrAdd(type, AsmCodegen<TAsm, TBuilder>.GenerateGetBuilder);
        return ref getBuilder(ref obj);
    }

    public static void SetBuilder(ref TAsm obj, TBuilder builder)
    {
        var type = obj.GetType();
        var setBuilder = setBuilderInstances.GetOrAdd(type, AsmCodegen<TAsm, TBuilder>.GenerateSetBuilder);
        setBuilder(ref obj, builder);
    }

    public static TAsm Clone(TAsm obj)
    {
        var type = obj.GetType();
        var clone = cloneInstances.GetOrAdd(type, AsmCodegen<TAsm, TBuilder>.GenerateClone);
        return clone(obj);
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
        AsmUtils<IAsyncStateMachine, int>.Equals(x, y);

    public int GetHashCode([DisallowNull] IAsyncStateMachine obj) =>
        AsmUtils<IAsyncStateMachine, int>.GetHashCode(obj);
}

/// <summary>
/// Generates async state machine manipulation code at runtime.
/// </summary>
/// <typeparam name="TAsm">The viewed async state machine type, which might be a concrete type or an abstract
/// one.</typeparam>
/// <typeparam name="TBuilder">The builder type of the state machine.</typeparam>
internal static class AsmCodegen<TAsm, TBuilder>
    where TAsm : IAsyncStateMachine
{
    /// <summary>
    /// A delegate type that checks equality between two non-null async state machines.
    /// </summary>
    /// <param name="x">The first state machine to compare.</param>
    /// <param name="y">The second state machine to compare.</param>
    /// <returns>True, if the two async state machines are equal.</returns>
    public delegate bool AsmEqualsDelegate(TAsm x, TAsm y);

    /// <summary>
    /// A delegate type that computes the hash value of a non-null async state machine.
    /// </summary>
    /// <param name="obj">The state machine to compute the hash of.</param>
    /// <returns>The hash value of the async state machine.</returns>
    public delegate int AsmGetHashCodeDelegate(TAsm obj);

    /// <summary>
    /// A delegate type that gets the builder for an async state machine.
    /// </summary>
    /// <param name="obj">The state machine to get the builder from.</param>
    public delegate ref TBuilder AsmGetBuilderDelegate(ref TAsm obj);

    /// <summary>
    /// A delegate type that sets the builder for an async state machine.
    /// </summary>
    /// <param name="obj">The state machine to assign the builder to.</param>
    /// <param name="builder">The builder to assign.</param>
    public delegate void AsmSetBuilderDelegate(ref TAsm obj, TBuilder builder);

    /// <summary>
    /// A delegate type that clones the state machine.
    /// </summary>
    /// <param name="obj">The state machine to clone.</param>
    /// <returns>The cloned async state machine.</returns>
    public delegate TAsm AsmCloneDelegate(TAsm obj);

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

    /// <summary>
    /// Generates an <see cref="AsmEqualsDelegate"/>.
    /// </summary>
    /// <param name="asmType">The exact async state machine type to generate equality for.</param>
    /// <returns>The generated delegate.</returns>
    public static AsmEqualsDelegate GenerateEquals(Type asmType)
    {
        var param1 = Expression.Parameter(typeof(TAsm));
        var param2 = Expression.Parameter(typeof(TAsm));

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
        var lambda = Expression.Lambda<AsmEqualsDelegate>(block, new[] { param1, param2 });

        // Compile
        return lambda.Compile();
    }

    /// <summary>
    /// Generates an <see cref="AsmGetHashCodeDelegate"/>.
    /// </summary>
    /// <param name="asmType">The exact async state machine type to generate hashing for.</param>
    /// <returns>The generated delegate.</returns>
    public static AsmGetHashCodeDelegate GenerateGetHashCode(Type asmType)
    {
        var param = Expression.Parameter(typeof(TAsm));

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
        var lambda = Expression.Lambda<AsmGetHashCodeDelegate>(block, param);

        // Compile
        return lambda.Compile();
    }

    /// <summary>
    /// Generates an <see cref="AsmGetBuilderDelegate"/>.
    /// </summary>
    /// <param name="asmType">The exact async state machine type to generate the getter for.</param>
    /// <returns>The generated delegate.</returns>
    public static AsmGetBuilderDelegate GenerateGetBuilder(Type asmType)
    {
        // Extract the field info
        var builderField = asmType.GetField("<>t__builder")!;

        if (builderField.FieldType != typeof(TBuilder))
        {
            throw new InvalidOperationException("When getting the builder, the exact builder type has to be known");
        }

        var asmParam = Expression.Parameter(typeof(TAsm).MakeByRefType());

        // Body
        var body = Expression.Field(
            GenerateCastAsmToExactType(asmParam, asmType),
            builderField);

        // Build up result
        var lambda = Expression.Lambda<AsmGetBuilderDelegate>(body, asmParam);

        // Compile
        return lambda.Compile();
    }

    /// <summary>
    /// Generates an <see cref="AsmSetBuilderDelegate"/>.
    /// </summary>
    /// <param name="asmType">The exact async state machine type to generate the setter for.</param>
    /// <returns>The generated delegate.</returns>
    public static AsmSetBuilderDelegate GenerateSetBuilder(Type asmType)
    {
        // Extract the field info
        var builderField = asmType.GetField("<>t__builder")!;

        if (builderField.FieldType != typeof(TBuilder))
        {
            throw new InvalidOperationException("When setting the builder, the exact builder type has to be known");
        }

        var asmParam = Expression.Parameter(typeof(TAsm).MakeByRefType());
        var builderParam = Expression.Parameter(typeof(TBuilder));

        // Body
        var body = Expression.Assign(
            // asm.builder =
            Expression.Field(
                GenerateCastAsmToExactType(asmParam, asmType),
                builderField),
            // builder
            builderParam);

        // Build up result
        var lambda = Expression.Lambda<AsmSetBuilderDelegate>(body, new[] { asmParam, builderParam });

        // Compile
        return lambda.Compile();
    }

    /// <summary>
    /// Generates an <see cref="AsmCloneDelegate"/>.
    /// </summary>
    /// <param name="asmType">The exact async state machine type to generate cloning for.</param>
    /// <returns>The generated delegate.</returns>
    public static AsmCloneDelegate GenerateClone(Type asmType)
    {
        var param = Expression.Parameter(typeof(TAsm));
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
        var lambda = Expression.Lambda<AsmCloneDelegate>(result, param);

        // Compile
        return lambda.Compile();
    }

    private static Expression GenerateCastAsmToExactType(Expression asm, Type asmType)
    {
        // No need for casting
        if (typeof(TAsm) == asmType) return asm;
        // We need to cast, Unsafe.As for ref types
        return asmType.IsValueType
            ? Expression.Convert(asm, asmType)
            : Expression.Call(
                  type: typeof(Unsafe),
                  methodName: nameof(Unsafe.As),
                  typeArguments: new[] { asmType },
                  arguments: asm);
    }

    private static IEnumerable<FieldInfo> GetNonSpecialFields(Type asmType) => asmType
        .GetFields()
        .Where(f => !f.Name.Contains('<'));
}
