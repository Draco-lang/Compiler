using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    /// The <see cref="Location"/> this symbol was defined at.
    /// </summary>
    public Location Definition { get; }

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
    /// Any variable symbol.
    /// </summary>
    public interface IVariable : ISymbol
    {
        /// <summary>
        /// True, if this is a mutable variable.
        /// </summary>
        public bool IsMutable { get; }

        /// <summary>
        /// The type of the variable.
        /// </summary>
        public Type Type { get; }
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
    public interface IFunction : ISymbol
    {
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
        public Type.Function Type { get; }
    }
}

internal partial interface ISymbol
{
    /// <summary>
    /// Any member symbol.
    /// </summary>
    public interface IMember : ISymbol
    {
    }
}

// Implementations /////////////////////////////////////////////////////////////

// TODO
