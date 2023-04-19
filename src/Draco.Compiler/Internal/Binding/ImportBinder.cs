using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds imported symbols in a scope.
/// </summary>
internal sealed class ImportBinder : Binder
{
    /// <summary>
    /// The diagnostics produced during import resolution.
    /// </summary>
    public DiagnosticBag ImportDiagnostics { get; } = new();

    /// <summary>
    /// The import items this binder brings in.
    /// </summary>
    public ImmutableArray<ImportItem> ImportItems => this.importItems ??= this.BindImportItems(this.ImportDiagnostics);
    private ImmutableArray<ImportItem>? importItems;

    public override IEnumerable<Symbol> DeclaredSymbols => this.ImportItems.SelectMany(i => i.ImportedSymbols);

    public override SyntaxNode DeclaringSyntax { get; }

    public ImportBinder(Binder parent, SyntaxNode declaringSyntax)
        : base(parent)
    {
        this.DeclaringSyntax = declaringSyntax;
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

    private ImmutableArray<ImportItem> BindImportItems(DiagnosticBag importDiagnostics)
    {
        // TODO
        throw new NotImplementedException();
    }

    private ImmutableArray<Symbol> BuildImportedSymbols(DiagnosticBag diagnostics)
    {
        // Collect import syntaxes
        var importSyntaxes = new List<ImportDeclarationSyntax>();
        var hasNonImport = false;
        foreach (var syntax in BinderFacts.EnumerateNodesInSameScope(this.DeclaringSyntax))
        {
            if (syntax is DeclarationStatementSyntax) continue;

            if (syntax is ImportDeclarationSyntax importSyntax)
            {
                importSyntaxes.Add(importSyntax);
                if (hasNonImport)
                {
                    diagnostics.Add(Diagnostic.Create(
                        template: SymbolResolutionErrors.ImportNotAtTop,
                        location: syntax.Location));
                }
            }
            else
            {
                hasNonImport = hasNonImport || syntax
                    is DeclarationSyntax
                    or StatementSyntax
                    or ExpressionSyntax;
            }
        }
        // Collect imported symbols
        var result = ImmutableArray.CreateBuilder<Symbol>();
        foreach (var importSyntax in importSyntaxes)
        {
            var importedSymbol = this.BindImportPath(importSyntax.Path, diagnostics);
            if (importedSymbol.IsError)
            {
                // No-op, don't cascade
            }
            else if (importedSymbol is not ModuleSymbol)
            {
                diagnostics.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.IllegalImport,
                    location: importSyntax.Path.Location,
                    formatArgs: ImportPathToString(importSyntax.Path)));
            }
            else
            {
                result.AddRange(importedSymbol.Members);
            }
        }
        return result.ToImmutable();
    }

    private static string ImportPathToString(ImportPathSyntax syntax) => syntax switch
    {
        RootImportPathSyntax root => root.Name.Text,
        MemberImportPathSyntax mem => $"{ImportPathToString(mem.Accessed)}.{mem.Member.Text}",
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };
}
