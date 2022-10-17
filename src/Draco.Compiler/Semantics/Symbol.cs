using System.Collections.Generic;

namespace Draco.Compiler.Semantics;

/// <summary>
/// Defines semantic symbols.
/// </summary>
public static class Symbol
{
    /// <summary>
    /// A base symbol.
    /// </summary>
    public interface ISymbol
    {
        /// <summary>
        /// The name of the symbol.
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// A variable symbol.
    /// </summary>
    public interface IVariable : ISymbol
    {
        /// <summary>
        /// The type of the variable.
        /// </summary>
        IType Type { get; }

        /// <summary>
        /// The kind of variable.
        /// </summary>
        Mutability Mutability { get; }
    }

    /// <summary>
    /// A parameter symbol.
    /// </summary>
    public interface IParameter : IVariable
    {
        /// <summary>
        /// The function the parameter is a parameter to.
        /// </summary>
        IFunction Function { get; }
    }

    /// <summary>
    /// A function symbol.
    /// </summary>
    public interface IFunction : ISymbol
    {
        /// <summary>
        /// The parameters to the function.
        /// </summary>
        IReadOnlyList<IParameter> Parameters { get; }

        /// <summary>
        /// The return type of the function.
        /// </summary>
        IType ReturnType { get; }
    }

    /// <summary>
    /// A type symbol.
    /// </summary>
    public interface IType : ISymbol
    {
        // This currently doesn't do anything.
    }

    /// <summary>
    /// The mutability of a variable.
    /// </summary>
    public enum Mutability
    {
        /// <summary>
        /// The variable is mutable (<c>var</c>).
        /// </summary>
        Mutable,

        /// <summary>
        /// The variable is immutable (<c>val</c>).
        /// </summary>
        Immutable
    }
}
