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
/// A collection of delegates for interacting with strongly typed async state machines.
/// </summary>
/// <typeparam name="TAsm">The exact async state machine type.</typeparam>
/// <typeparam name="TBuilder">The builder type.</typeparam>
internal readonly struct AsmInterface<TAsm, TBuilder>
    where TAsm : IAsyncStateMachine
{
    // Exposed delegates generated
    public delegate bool EqualsDelegate(ref TAsm x, ref TAsm y);
    public delegate int GetHashCodeDelegate(ref TAsm obj);
    public delegate TAsm CloneDelegate(ref TAsm obj);
    public delegate ref TBuilder GetBuilderDelegate(ref TAsm obj);
    public delegate QueryDatabase GetQueryDatabaseDelegate(ref TAsm obj);

    // Static members needed for codegen
    private static readonly MethodInfo getTypeMethod;
    private static readonly MethodInfo equalsMethod;
    private static readonly MethodInfo toHashCodeMethod;
    private static readonly MethodInfo memberwiseCloneMethod;

    // Loading static members for codegen
    static AsmInterface()
    {
        getTypeMethod = typeof(object).GetMethod(nameof(GetType))!;
        equalsMethod = typeof(object).GetMethod(
            nameof(object.Equals),
            BindingFlags.Public | BindingFlags.Static)!;
        toHashCodeMethod = typeof(HashCode).GetMethod(nameof(HashCode.ToHashCode))!;
        memberwiseCloneMethod = typeof(object).GetMethod(
            nameof(MemberwiseClone),
            BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    public static AsmInterface<TAsm, TBuilder> Create() => new(
        equals: GenerateEquals(),
        getHashCode: GenerateGetHashCode(),
        clone: GenerateClone(),
        getBuilder: GenerateGetBuilder(),
        getQueryDatabase: GenerateGetQueryDatabase());

    // Members for the interface
    public readonly new EqualsDelegate Equals;
    public readonly new GetHashCodeDelegate GetHashCode;
    public readonly CloneDelegate Clone;
    public readonly GetBuilderDelegate GetBuilder;
    public readonly GetQueryDatabaseDelegate GetQueryDatabase;

    private AsmInterface(
        EqualsDelegate equals,
        GetHashCodeDelegate getHashCode,
        CloneDelegate clone,
        GetBuilderDelegate getBuilder,
        GetQueryDatabaseDelegate getQueryDatabase)
    {
        this.Equals = equals;
        this.GetHashCode = getHashCode;
        this.Clone = clone;
        this.GetBuilder = getBuilder;
        this.GetQueryDatabase = getQueryDatabase;
    }

    // Codegen code

    private static EqualsDelegate GenerateEquals()
    {
        var param1 = Expression.Parameter(typeof(TAsm).MakeByRefType());
        var param2 = Expression.Parameter(typeof(TAsm).MakeByRefType());

        // Generate each individual comparison between fields
        var comparisons = GetNonSpecialFields()
            .Select(f => Expression.Call(
                method: equalsMethod,
                arguments: new[]
                {
                    Expression.Convert(Expression.Field(param1, f), typeof(object)),
                    Expression.Convert(Expression.Field(param2, f), typeof(object)),
                }));

        // And them together, add a constant true at the start to make 0-field ones correct code
        var comparisonsAnded = comparisons
           .Cast<Expression>()
           .Aggregate(Expression.Constant(true) as Expression, Expression.AndAlso);

        // Build up result
        var lambda = Expression.Lambda<EqualsDelegate>(comparisonsAnded, new[] { param1, param2 });

        // Compile
        return lambda.Compile();
    }

    private static GetHashCodeDelegate GenerateGetHashCode()
    {
        var param = Expression.Parameter(typeof(TAsm).MakeByRefType());

        var hashCode = Expression.Variable(typeof(HashCode));
        var blockExprs = new List<Expression>()
        {
            // var hashCode = default(HashCode)
            Expression.Assign(hashCode, Expression.Default(typeof(HashCode))),
        };

        foreach (var field in GetNonSpecialFields())
        {
            // hashCode.Add(asm.Field)
            blockExprs.Add(Expression.Call(
                instance: hashCode,
                methodName: nameof(HashCode.Add),
                typeArguments: new[] { field.FieldType },
                arguments: Expression.Field(param, field)));
        }

        // hashCode.ToHashCode()
        blockExprs.Add(Expression.Call(
            instance: hashCode,
            method: toHashCodeMethod));

        // Build up result
        var block = Expression.Block(
            variables: new[] { hashCode },
            expressions: blockExprs);
        var lambda = Expression.Lambda<GetHashCodeDelegate>(block, param);

        // Compile
        return lambda.Compile();
    }

    private static CloneDelegate GenerateClone()
    {
        var param = Expression.Parameter(typeof(TAsm).MakeByRefType());

        // For ref types we need to invoke memberwise clone
        var result = typeof(TAsm).IsValueType
            ? param as Expression
            : Expression.Convert(Expression.Call(param, memberwiseCloneMethod), typeof(TAsm));

        // Build up result
        var lambda = Expression.Lambda<CloneDelegate>(result, param);

        // Compile
        return lambda.Compile();
    }

    private static GetBuilderDelegate GenerateGetBuilder()
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

        return method.CreateDelegate<GetBuilderDelegate>();
    }

    private static GetQueryDatabaseDelegate GenerateGetQueryDatabase()
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
        var lambda = Expression.Lambda<GetQueryDatabaseDelegate>(body, param);

        // Compile
        return lambda.Compile();
    }

    private static IEnumerable<FieldInfo> GetNonSpecialFields() => typeof(TAsm)
        .GetFields()
        .Where(f => !f.Name.Contains('<'));
}
