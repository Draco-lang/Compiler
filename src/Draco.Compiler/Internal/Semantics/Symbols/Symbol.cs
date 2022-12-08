using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Semantics.Types;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using IApiSymbol = Draco.Compiler.Api.Semantics.ISymbol;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Internal.Semantics.Symbols;

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
    /// The <see cref="ParseTree"/> this symbol was defined by.
    /// </summary>
    public ParseTree? Definition { get; }

    /// <summary>
    /// True, if the symbol is visible externally.
    /// </summary>
    public bool IsExternallyVisible { get; }

    /// <summary>
    /// True, if this is a global symbol.
    /// </summary>
    public bool IsGlobal { get; }

    /// <summary>
    /// Converts this <see cref="ISymbol"/> to an <see cref="IApiSymbol"/>.
    /// </summary>
    /// <returns>The equivalent <see cref="IApiSymbol"/>.</returns>
    public IApiSymbol ToApiSymbol();
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
    /// Any operator symbol.
    /// </summary>
    public interface IOperator : ISymbol
    {
        /// <summary>
        /// The type of this operator as a function type.
        /// </summary>
        public Type.Function FunctionType { get; }

        /// <summary>
        /// The type the operator results in.
        /// </summary>
        public Type ResultType { get; set; }
    }

    /// <summary>
    /// Any unary operator symbol.
    /// </summary>
    public interface IUnaryOperator : IOperator
    {
        /// <summary>
        /// The operand type.
        /// </summary>
        public Type OperandType { get; }
    }

    /// <summary>
    /// Any binary operator symbol.
    /// </summary>
    public interface IBinaryOperator : IOperator
    {
        /// <summary>
        /// The left operand type.
        /// </summary>
        public Type LeftOperandType { get; }

        /// <summary>
        /// The right operand type.
        /// </summary>
        public Type RightOperandType { get; }
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

internal partial interface ISymbol
{
    // Base helper class for symbols that are materialized in the tree
    private abstract class InTreeBase : ISymbol
    {
        public string Name { get; }
        public ParseTree Definition { get; }
        public bool IsError => false;
        public ImmutableArray<Diagnostic> Diagnostics => ImmutableArray<Diagnostic>.Empty;
        public IScope DefiningScope => SymbolResolution.GetContainingScopeOrNull(this.db, this.Definition)
                                    ?? throw new InvalidOperationException();
        public virtual IFunction? DefiningFunction
        {
            get
            {
                // Walk up until we hit a function scope
                var scope = this.DefiningScope;
                do
                {
                    scope = scope.Parent;
                    if (scope is null) return null;
                }
                while (!scope.IsFunction);

                var funcDef = scope.Definition;
                Debug.Assert(funcDef is not null);

                var funcSymbol = SymbolResolution.GetDefinedSymbolOrNull(this.db, funcDef);
                Debug.Assert(funcSymbol is not null);

                return (IFunction)funcSymbol;
            }
        }
        public virtual bool IsExternallyVisible => false;
        public bool IsGlobal => (this.DefiningScope?.Kind ?? ScopeKind.Global) == ScopeKind.Global;

        private readonly QueryDatabase db;

        protected InTreeBase(QueryDatabase db, string name, ParseTree definition)
        {
            this.db = db;
            this.Name = name;
            this.Definition = definition;
        }

        public IApiSymbol ToApiSymbol() => throw new NotImplementedException();
    }
}
