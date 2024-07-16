using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.OverloadResolution;

/// <summary>
/// Utilities for calls and overload resolution.
/// </summary>
internal static class CallUtilities
{
    /// <summary>
    /// Checks, if a function matches a given parameter count.
    /// </summary>
    /// <param name="function">The function to check.</param>
    /// <param name="argc">The number of arguments passed in.</param>
    /// <returns>True, if <paramref name="function"/> can be called with <paramref name="argc"/> number
    /// of arguments.</returns>
    public static bool MatchesParameterCount(FunctionSymbol function, int argc)
    {
        // Exact count match is always eligibe by only param count
        if (function.Parameters.Length == argc) return true;
        // If not variadic, we do need an exact match
        if (!function.IsVariadic) return false;
        // Otherise, there must be one less, exactly as many, or more arguments
        //  - one less means nullary variadics
        //  - exact match is one variadic
        //  - more is more variadics
        if (argc + 1 >= function.Parameters.Length) return true;
        // No match
        return false;
    }
}
