using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds imported symbols in a scope.
/// </summary>
internal sealed class ImportBinder : Binder
{
    public override IEnumerable<Symbol> DeclaredSymbols => this.importedSymbols ??= this.BuildImportedSymbols();

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

    private ImmutableArray<Symbol> BuildImportedSymbols()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();
        foreach (var importSyntax in this.importSyntaxes)
        {
            var path = importSyntax.Path.Values
                .Select(v => v.Text)
                .ToImmutableArray();
            var symbols = this.ImportPath(path, importSyntax);
            result.AddRange(symbols);
        }
        return result.ToImmutable();
    }

    private IEnumerable<Symbol> ImportPath(ImmutableArray<string> path, SyntaxNode reference)
    {
        if (path.Length == 0) return Enumerable.Empty<Symbol>();

        // TODO: We are dumping into global diagnostics, maybe that's not good...
        var diagnostics = this.Compilation.GlobalDiagnosticBag;
        // We look up the first component
        // NOTE: We don't start the lookup here, as this binder scope is currently being constructed
        // Calling this.LookupValueSymbol would cause recursion
        Debug.Assert(this.Parent is not null);
        var result = this.Parent.LookupValueSymbol(path[0], reference, diagnostics);
        // Based on the result we start to look up the sub-elements
        for (var i = 1; i < path.Length; ++i)
        {
            var pathElement = path[i];
            if (result is ModuleSymbol module)
            {
                // Search for the elements in the module with that name
                var membersWithName = module.Members
                    .Where(m => m.Name == pathElement);
                // Construct a result from it
                var lookupResult = LookupResult.FromResultSet(membersWithName);
                // Step forward in the chain
                result = lookupResult.GetValue(pathElement, reference, diagnostics);
            }
            else
            {
                // TODO
                throw new NotImplementedException();
            }
        }
        // We are at the end of the chain, we got something in result
        if (result is ModuleSymbol importedModule)
        {
            // We imported a module, we need its contents
            return importedModule.Members;
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
