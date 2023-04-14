using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds imported symbols in a scope.
/// </summary>
internal sealed class ImportBinder : Binder
{
    public override IEnumerable<Symbol> DeclaredSymbols =>
        this.importedSymbols ??= this.BuildImportedSymbols(this.Compilation.GlobalDiagnosticBag);

    public override SyntaxNode DeclaringSyntax { get; }

    private readonly ImmutableArray<ImportDeclarationSyntax> importSyntaxes;
    private ImmutableArray<Symbol>? importedSymbols;

    public ImportBinder(
        Binder parent,
        SyntaxNode declaringSyntax,
        ImmutableArray<ImportDeclarationSyntax> importSyntaxes)
        : base(parent)
    {
        this.DeclaringSyntax = declaringSyntax;
        this.importSyntaxes = importSyntaxes;
    }

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        foreach (var symbol in this.DeclaredSymbols)
        {
            if (symbol.Name != name) continue;
            if (!allowSymbol(symbol)) continue;
            result.Add(symbol);
        }
    }

    private ImmutableArray<Symbol> BuildImportedSymbols(DiagnosticBag diagnostics)
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();
        foreach (var importSyntax in this.importSyntaxes)
        {
            var importedSymbol = this.BindImportPath(importSyntax.Path, diagnostics);
            if (importedSymbol is not ModuleSymbol)
            {
                // TODO: Error
                throw new NotImplementedException();
            }
            else
            {
                result.AddRange(importedSymbol.Members);
            }
        }
        return result.ToImmutable();
    }
}
