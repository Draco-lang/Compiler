using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An interface for all symbols defined in-source.
/// </summary>
internal interface ISourceSymbol
{
    /// <summary>
    /// Enforced binding of the symbol. It does not recurse to bind members of the symbol.
    /// </summary>
    /// <param name="binderProvider">The provider to get binders from.</param>
    public void Bind(IBinderProvider binderProvider);
}
