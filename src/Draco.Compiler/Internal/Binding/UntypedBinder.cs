using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Constructs the untyped tree from the syntax tree.
/// </summary>
internal abstract partial class UntypedBinder
{
    /// <summary>
    /// A filter delegate for symbols.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>True, if the filter accepts <paramref name="symbol"/>.</returns>
    protected delegate bool SymbolFilter(Symbol symbol);

    /// <summary>
    /// The binder that gets invoked, if something could not be resolved in this one.
    /// In other terms, this is the parent scope.
    /// </summary>
    protected UntypedBinder? Parent { get; }

    /// <summary>
    /// Binds the given syntaxes to a symbol representing the entire module for compilation.
    /// </summary>
    /// <param name="syntaxes">The <see cref="CompilationUnitSyntax"/>es to bind.</param>
    /// <returns>The bound <see cref="ModuleSymbol"/> with potential type-info still missing.</returns>
    public static ModuleSymbol Bind(IEnumerable<CompilationUnitSyntax> syntaxes)
    {
        throw new NotImplementedException();
    }
}
