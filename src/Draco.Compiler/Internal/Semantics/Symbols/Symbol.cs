using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Types;
using IApiSymbol = Draco.Compiler.Api.Semantics.ISymbol;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Internal.Semantics.Symbols;

// Factory /////////////////////////////////////////////////////////////////////

internal static partial class Symbol
{
    // TODO: Maybe error factories could construct the diags themselves?
    public static ISymbol MakeReferenceError(string name, ImmutableArray<Diagnostic> diagnostics) =>
        new ReferenceError(name, diagnostics);

    public static ISymbol.ILabel MakeLabel(QueryDatabase db, string name, SyntaxNode definition) =>
        new Label(db, name, definition, ImmutableArray<Diagnostic>.Empty);

    public static ISymbol.ILabel SynthetizeLabel(string? name = null) =>
        new SynthetizedLabel(name);

    public static ISymbol.IVariable MakeVariable(QueryDatabase db, string name, SyntaxNode definition, bool isMutable) =>
        new Variable(db, name, definition, ImmutableArray<Diagnostic>.Empty, isMutable);

    public static ISymbol.IVariable SynthetizeVariable(Type type, bool isMutable) =>
        new SynthetizedVariable(type, isMutable);

    public static ISymbol.IParameter MakeParameter(QueryDatabase db, string name, SyntaxNode definition) =>
        new Parameter(db, name, definition, ImmutableArray<Diagnostic>.Empty);

    public static ISymbol.IParameter SynthetizeParameter(Type type) =>
        new SynthetizedParameter(type);

    public static ISymbol.IFunction MakeFunction(QueryDatabase db, string name, SyntaxNode definition) =>
        new Function(db, name, definition, ImmutableArray<Diagnostic>.Empty);

    public static ISymbol.IOverloadSet SynthetizeOverloadSet(ImmutableArray<ISymbol.IFunction> functions) =>
        new OverloadSet(functions[0].Name, functions);

    public static ISymbol.IOverloadSet SynthetizeOverloadSet(ISymbol.IOverloadSet f1, ImmutableArray<ISymbol.IFunction> f2)
    {
        Debug.Assert(f2.All(f => f1.Name == f.Name));
        return new OverloadSet(f1.Name, f1.Functions.AddRange(f2));
    }

    public static ISymbol.IFunction MakeIntrinsicFunction(string name, ImmutableArray<Type> paramTypes, Type returnType) =>
        new IntrinsicFunction(name, paramTypes, returnType);

    public static ISymbol.IFunction MakeIntrinsicUnaryOperator(TokenType op, Type operandType, Type resultType) =>
        new IntrinsicFunction(
            SymbolResolution.GetUnaryOperatorName(op),
            ImmutableArray.Create(operandType),
            resultType);

    public static ISymbol.IFunction MakeIntrinsicBinaryOperator(
        TokenType op, Type leftOperandType, Type rightrOperandType, Type resultType) =>
        new IntrinsicFunction(
            SymbolResolution.GetBinaryOperatorName(op) ?? throw new ArgumentOutOfRangeException(nameof(op)),
            ImmutableArray.Create(leftOperandType, rightrOperandType),
            resultType);

    public static ISymbol.IFunction MakeIntrinsicRelationalOperator(
        TokenType op, Type leftOperandType, Type rightrOperandType, Type resultType) =>
        new IntrinsicFunction(
            SymbolResolution.GetRelationalOperatorName(op) ?? throw new ArgumentOutOfRangeException(nameof(op)),
            ImmutableArray.Create(leftOperandType, rightrOperandType),
            resultType);

    public static ISymbol.ITypeDefinition MakeIntrinsicTypeDefinition(string name, Type type) =>
        new IntrinsicTypeDefinition(name, type);

    private static string ExtractDocumentation(SyntaxNode? definition)
    {
        if (definition is null) return string.Empty;
        // Get all the doc commemts above the declaration
        var token = definition.Tokens.FirstOrDefault();
        var trivia = token?.LeadingTrivia.Where(x => x.Type == TriviaType.DocumentationComment);
        // Concatenate the text of all the doc comments
        return trivia is not null ? string.Join(Environment.NewLine, trivia.Select(x => x.Text.Remove(0, 3))) : string.Empty;
    }
}

// Interfaces //////////////////////////////////////////////////////////////////

/// <summary>
/// The interface of all symbols.
/// </summary>
internal partial interface ISymbol
{
    /// <summary>
    /// The name of this symbol that it can be referenced by.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// True, if this symbol references some kind of error.
    /// </summary>
    public bool IsError { get; }

    /// <summary>
    /// True, if this is an intrinsic.
    /// </summary>
    public bool IsIntrinsic { get; }

    /// <summary>
    /// The diagnostics attached to this symbol.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// The <see cref="IScope"/> that this symbol was defined in.
    /// </summary>
    public IScope? DefiningScope { get; }

    /// <summary>
    /// The <see cref="IFunction"/> that this symbol was defined in.
    /// </summary>
    public IFunction? DefiningFunction { get; }

    /// <summary>
    /// The <see cref="SyntaxNode"/> this symbol was defined by.
    /// </summary>
    public SyntaxNode? Definition { get; }

    /// <summary>
    /// True, if the symbol is visible externally.
    /// </summary>
    public bool IsExternallyVisible { get; }

    /// <summary>
    /// True, if this is a global symbol.
    /// </summary>
    public bool IsGlobal { get; }

    /// <summary>
    /// Documentation attached to this symbol.
    /// </summary>
    public string Documentation { get; }

    /// <summary>
    /// Converts this <see cref="ISymbol"/> to an <see cref="IApiSymbol"/>.
    /// </summary>
    /// <returns>The equivalent <see cref="IApiSymbol"/>.</returns>
    public IApiSymbol ToApiSymbol();

    /// <summary>
    /// Appends <see cref="Diagnostic"/>s to this <see cref="ISymbol"/>, creating a new one.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to append.</param>
    /// <returns>A copy of this symbol with <paramref name="diagnostics"/> appended.</returns>
    public ISymbol WithDiagnostics(ImmutableArray<Diagnostic> diagnostics);
}

internal partial interface ISymbol
{
    /// <summary>
    /// Any symbol that has a meaningful type, when referenced.
    /// </summary>
    public interface ITyped : ISymbol
    {
        /// <summary>
        /// The type of the symbol.
        /// </summary>
        public Type Type { get; }
    }
}

internal partial interface ISymbol
{
    /// <summary>
    /// Any variable symbol.
    /// </summary>
    public interface IVariable : ITyped
    {
        /// <summary>
        /// True, if this is a mutable variable.
        /// </summary>
        public bool IsMutable { get; }
    }
}

internal partial interface ISymbol
{
    /// <summary>
    /// Any label symbol.
    /// </summary>
    public interface ILabel : ISymbol
    {
    }
}

internal partial interface ISymbol
{
    /// <summary>
    /// Any function parameter symbol.
    /// </summary>
    public interface IParameter : IVariable
    {
    }
}

internal partial interface ISymbol
{
    /// <summary>
    /// Any function symbol.
    /// </summary>
    public interface IFunction : ITyped
    {
        /// <summary>
        /// The scope this function introduces.
        /// </summary>
        public IScope? DefinedScope { get; }

        /// <summary>
        /// The parameters of this function.
        /// </summary>
        public ImmutableArray<IParameter> Parameters { get; }

        /// <summary>
        /// The return type of this function.
        /// </summary>
        public Type ReturnType { get; }

        /// <summary>
        /// The type of the function.
        /// </summary>
        public new Type.Function Type { get; }
    }
}

internal partial interface ISymbol
{
    /// <summary>
    /// A set of functions that are overloaded.
    /// </summary>
    public interface IOverloadSet : ISymbol
    {
        /// <summary>
        /// The functions that participate in the overload.
        /// </summary>
        public ImmutableArray<IFunction> Functions { get; }
    }
}

internal partial interface ISymbol
{
    /// <summary>
    /// Any member symbol.
    /// </summary>
    public interface IMember : ITyped
    {
    }
}

internal partial interface ISymbol
{
    /// <summary>
    /// A type definition symbol.
    /// </summary>
    public interface ITypeDefinition : ISymbol
    {
        /// <summary>
        /// The type that is defined.
        /// </summary>
        public Type DefinedType { get; }
    }
}

// Implementations /////////////////////////////////////////////////////////////

internal static partial class Symbol
{
    // Base for errors
    private abstract class ErrorBase : ISymbol
    {
        public string Name { get; }
        public bool IsError => true;
        public bool IsIntrinsic => false;
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public virtual IScope? DefiningScope => null;
        public virtual ISymbol.IFunction? DefiningFunction => null;
        public virtual SyntaxNode? Definition => null;
        public bool IsExternallyVisible => false;
        public bool IsGlobal => false;
        public string Documentation => string.Empty;

        protected ErrorBase(string name, ImmutableArray<Diagnostic> diagnostics)
        {
            this.Name = name;
            this.Diagnostics = diagnostics;
        }

        public IApiSymbol ToApiSymbol() => new ErrorSymbol(this);
        public abstract ISymbol WithDiagnostics(ImmutableArray<Diagnostic> diagnostics);
    }
}

internal static partial class Symbol
{
    // Base helper class for symbols that are materialized in the tree
    private abstract class InTreeBase : ISymbol
    {
        public string Name { get; }
        public SyntaxNode Definition { get; }
        public bool IsError => this.Diagnostics.Length > 0;
        public bool IsIntrinsic => false;
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public IScope DefiningScope
        {
            get
            {
                var result = SymbolResolution.GetContainingScopeOrNull(this.db, this.Definition);
                Debug.Assert(result is not null);
                return result!;
            }
        }
        public virtual ISymbol.IFunction? DefiningFunction
        {
            get
            {
                // Walk up until we hit a function scope
                var scope = this.DefiningScope;
                while (!scope.IsFunction)
                {
                    scope = scope.Parent;
                    if (scope is null) return null;
                }

                var funcDef = scope.Definition;
                Debug.Assert(funcDef is not null);

                var funcSymbol = SymbolResolution.GetDefinedSymbolExpected<ISymbol.IFunction>(this.db, funcDef!);
                return funcSymbol;
            }
        }
        public virtual bool IsExternallyVisible => false;
        public bool IsGlobal => this.DefiningScope.IsGlobal;
        public string Documentation => string.Empty;

        protected readonly QueryDatabase db;

        protected InTreeBase(
            QueryDatabase db,
            string name,
            SyntaxNode definition,
            ImmutableArray<Diagnostic> diagnostics)
        {
            this.db = db;
            this.Name = name;
            this.Definition = definition;
            this.Diagnostics = diagnostics;
        }

        public abstract IApiSymbol ToApiSymbol();
        public abstract ISymbol WithDiagnostics(ImmutableArray<Diagnostic> diagnostics);
    }
}

internal static partial class Symbol
{
    // Base helper class for symbols that are synthetized
    private abstract class SynthetizedBase : ISymbol
    {
        private static int instanceCounter = -1;

        protected static string GenerateName(string? hint) =>
            $"{hint ?? "synthetized"}<{Interlocked.Increment(ref instanceCounter)}>";

        public string Name { get; }
        public bool IsError => false;
        public virtual bool IsIntrinsic => false;
        public ImmutableArray<Diagnostic> Diagnostics => ImmutableArray<Diagnostic>.Empty;
        public IScope? DefiningScope => null;
        public ISymbol.IFunction? DefiningFunction => null;
        public SyntaxNode? Definition => null;
        public virtual bool IsExternallyVisible => false;
        public virtual bool IsGlobal => false;
        public string Documentation => string.Empty;

        protected SynthetizedBase(string name)
        {
            this.Name = name;
        }

        public abstract IApiSymbol ToApiSymbol();
        public ISymbol WithDiagnostics(ImmutableArray<Diagnostic> diagnostics) => throw new NotSupportedException();
    }
}

internal static partial class Symbol
{
    /// <summary>
    /// A symbol for a reference error.
    /// </summary>
    private sealed class ReferenceError : ErrorBase, ISymbol.IVariable, ISymbol.ILabel, ISymbol.ITypeDefinition
    {
        public Type Type => Type.Error.Empty;
        public Type DefinedType => Type.Error.Empty;
        public bool IsMutable => true;

        public ReferenceError(string name, ImmutableArray<Diagnostic> diagnostics)
            : base(name, diagnostics)
        {
        }

        public override ISymbol WithDiagnostics(ImmutableArray<Diagnostic> diagnostics) =>
            new ReferenceError(this.Name, this.Diagnostics.Concat(diagnostics).ToImmutableArray());
    }
}

internal static partial class Symbol
{
    /// <summary>
    /// A symbol for user-defined labels.
    /// </summary>
    private sealed class Label : InTreeBase, ISymbol.ILabel
    {
        public Label(QueryDatabase db, string name, SyntaxNode definition, ImmutableArray<Diagnostic> diagnostics)
            : base(db, name, definition, diagnostics)
        {
        }

        public override IApiSymbol ToApiSymbol() => new LabelSymbol(this);
        public override ISymbol WithDiagnostics(ImmutableArray<Diagnostic> diagnostics) =>
            new Label(this.db, this.Name, this.Definition, this.Diagnostics.Concat(diagnostics).ToImmutableArray());
    }
}

internal static partial class Symbol
{
    /// <summary>
    /// A symbol for synthetized labels.
    /// </summary>
    private sealed class SynthetizedLabel : SynthetizedBase, ISymbol.ILabel
    {
        public SynthetizedLabel(string? name)
            : base(name ?? GenerateName("label"))
        {
        }

        public override IApiSymbol ToApiSymbol() => new LabelSymbol(this);
    }
}

internal static partial class Symbol
{
    /// <summary>
    /// A symbol for user-defined variables.
    /// </summary>
    private sealed class Variable : InTreeBase, ISymbol.IVariable
    {
        public override bool IsExternallyVisible => this.IsGlobal;
        public bool IsMutable { get; }
        public Type Type => TypeChecker.TypeOf(this.db, this).UnwrapTypeVariable;
        public new string Documentation => ExtractDocumentation(this.Definition);

        public Variable(QueryDatabase db, string name, SyntaxNode definition, ImmutableArray<Diagnostic> diagnostics, bool isMutable)
            : base(db, name, definition, diagnostics)
        {
            this.IsMutable = isMutable;
        }

        public override IApiSymbol ToApiSymbol() => new VariableSymbol(this);
        public override ISymbol WithDiagnostics(ImmutableArray<Diagnostic> diagnostics) =>
            new Variable(this.db, this.Name, this.Definition, this.Diagnostics.Concat(diagnostics).ToImmutableArray(), this.IsMutable);
    }
}

internal static partial class Symbol
{
    /// <summary>
    /// A symbol for synthetized variables.
    /// </summary>
    private sealed class SynthetizedVariable : SynthetizedBase, ISymbol.IVariable
    {
        public Type Type { get; }
        public bool IsMutable { get; }
        public new string Documentation => ExtractDocumentation(this.Definition);

        public SynthetizedVariable(Type type, bool isMutable)
            : base(GenerateName("variable"))
        {
            this.Type = type;
            this.IsMutable = isMutable;
        }

        public override IApiSymbol ToApiSymbol() => new VariableSymbol(this);
    }
}

internal static partial class Symbol
{
    /// <summary>
    /// A symbol for user-defined parameters.
    /// </summary>
    private sealed class Parameter : InTreeBase, ISymbol.IParameter
    {
        public bool IsMutable => false;
        public Type Type => TypeChecker.TypeOf(this.db, this);

        public Parameter(QueryDatabase db, string name, SyntaxNode definition, ImmutableArray<Diagnostic> diagnostics)
            : base(db, name, definition, diagnostics)
        {
        }

        public override IApiSymbol ToApiSymbol() => new ParameterSymbol(this);
        public override ISymbol WithDiagnostics(ImmutableArray<Diagnostic> diagnostics) =>
            new Parameter(this.db, this.Name, this.Definition, this.Diagnostics.Concat(diagnostics).ToImmutableArray());
    }
}

internal static partial class Symbol
{
    /// <summary>
    /// A symbol for synthetized parameters.
    /// </summary>
    private sealed class SynthetizedParameter : SynthetizedBase, ISymbol.IParameter
    {
        public bool IsMutable => false;
        public Type Type { get; }

        public SynthetizedParameter(Type type)
            : base(GenerateName("parameter"))
        {
            this.Type = type;
        }

        public override IApiSymbol ToApiSymbol() => new ParameterSymbol(this);
    }
}

internal static partial class Symbol
{
    /// <summary>
    /// A symbol for user-defined functions.
    /// </summary>
    private sealed class Function : InTreeBase, ISymbol.IFunction
    {
        public override bool IsExternallyVisible => this.IsGlobal;
        public new string Documentation => ExtractDocumentation(this.Definition);
        public IScope DefinedScope
        {
            get
            {
                var scope = SymbolResolution.GetDefinedScopeOrNull(this.db, this.Definition);
                Debug.Assert(scope is not null);
                return scope!;
            }
        }
        public ImmutableArray<ISymbol.IParameter> Parameters
        {
            get
            {
                var builder = ImmutableArray.CreateBuilder<ISymbol.IParameter>();
                var tree = (FunctionDeclarationSyntax)this.Definition;
                foreach (var param in tree.ParameterList.Value.Elements)
                {
                    var symbol = SymbolResolution.GetDefinedSymbolOrNull(this.db, param.Value);
                    Debug.Assert(symbol is ISymbol.IParameter);
                    builder.Add((ISymbol.IParameter)symbol!);
                }
                return builder.ToImmutable();
            }
        }
        public Type ReturnType
        {
            get
            {
                var tree = (FunctionDeclarationSyntax)this.Definition;
                return tree.ReturnType is null
                    ? Types.Type.Unit
                    : TypeChecker.Evaluate(this.db, tree.ReturnType.Type);
            }
        }
        public Type.Function Type => new(
            this.Parameters.Select(p => p.Type).ToImmutableArray(),
            this.ReturnType);
        Type ISymbol.ITyped.Type => this.Type;

        public Function(QueryDatabase db, string name, SyntaxNode definition, ImmutableArray<Diagnostic> diagnostics)
            : base(db, name, definition, diagnostics)
        {
        }

        public override IApiSymbol ToApiSymbol() => new FunctionSymbol(this);
        public override ISymbol WithDiagnostics(ImmutableArray<Diagnostic> diagnostics) =>
            new Function(this.db, this.Name, this.Definition, this.Diagnostics.Concat(diagnostics).ToImmutableArray());
    }
}

internal static partial class Symbol
{
    /// <summary>
    /// A set of overloaded functions.
    /// </summary>
    private sealed class OverloadSet : SynthetizedBase, ISymbol.IOverloadSet
    {
        public ImmutableArray<ISymbol.IFunction> Functions { get; }

        public OverloadSet(string name, ImmutableArray<ISymbol.IFunction> functions)
            : base(name)
        {
            this.Functions = functions;
        }

        public override IApiSymbol ToApiSymbol() => throw new NotImplementedException();
    }
}

internal static partial class Symbol
{
    /// <summary>
    /// A symbol for intrinsic functions implemented by the compiler.
    /// </summary>
    private sealed class IntrinsicFunction : SynthetizedBase, ISymbol.IFunction
    {
        public override bool IsExternallyVisible => true;
        public override bool IsIntrinsic => true;
        public IScope? DefinedScope => null;
        public ImmutableArray<ISymbol.IParameter> Parameters { get; }
        public Type ReturnType { get; }
        public new string Documentation => ExtractDocumentation(this.Definition);
        public Type.Function Type => new(
            this.Parameters.Select(p => p.Type).ToImmutableArray(),
            this.ReturnType);
        Type ISymbol.ITyped.Type => this.Type;

        public IntrinsicFunction(string name, ImmutableArray<Type> paramTypes, Type returnType)
            : base(name)
        {
            this.Parameters = paramTypes
                .Select(SynthetizeParameter)
                .ToImmutableArray();
            this.ReturnType = returnType;
        }

        public override IApiSymbol ToApiSymbol() => new FunctionSymbol(this);
    }
}

internal static partial class Symbol
{
    /// <summary>
    /// Intrinsic primitive types.
    /// </summary>
    private sealed class IntrinsicTypeDefinition : SynthetizedBase, ISymbol.ITypeDefinition
    {
        public override bool IsIntrinsic => true;
        public Type DefinedType { get; }

        public IntrinsicTypeDefinition(string name, Type definedType)
            : base(name)
        {
            this.DefinedType = definedType;
        }

        public override IApiSymbol ToApiSymbol() => new TypeSymbol(this);
    }
}
