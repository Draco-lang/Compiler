namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// The different arithmetic operations supported.
/// </summary>
internal enum ArithmeticOp
{
    /// <summary>
    /// Arithmetic addition.
    /// </summary>
    Add,

    /// <summary>
    /// Arithmetic subtraction.
    /// </summary>
    Sub,

    /// <summary>
    /// Arithmetic multiplication.
    /// </summary>
    Mul,

    /// <summary>
    /// Arithmetic division.
    /// </summary>
    Div,

    /// <summary>
    /// Arithmetic remainder.
    /// </summary>
    Rem,

    /// <summary>
    /// Less-than comparison.
    /// </summary>
    Less,

    /// <summary>
    /// Equality comparison.
    /// </summary>
    Equal,
}
