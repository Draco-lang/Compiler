using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds imported symbols in a scope.
/// </summary>
internal sealed class ImportBinder : Binder
{
    public override IEnumerable<Symbol> DeclaredSymbols => this.importedSymbols ??= this.BuildImportedSymbols();

    private readonly ImmutableArray<ImportDeclarationSyntax> importSyntaxes;
    private ImmutableArray<Symbol>? importedSymbols;

    public ImportBinder(Binder parent, ImmutableArray<ImportDeclarationSyntax> importSyntaxes)
        : base(parent)
    {
        this.importSyntaxes = importSyntaxes;
    }

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        foreach (var symbol in this.DeclaredSymbols)
        {
            if (symbol.Name != name) continue;
            if (!allowSymbol(symbol)) continue;
            result.Add(symbol);
            break;
        }
    }

    private ImmutableArray<Symbol> BuildImportedSymbols()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();
        foreach (var importSyntax in this.importSyntaxes)
        {
            // TODO
            throw new NotImplementedException();
        }
        return result.ToImmutable();
    }
}
