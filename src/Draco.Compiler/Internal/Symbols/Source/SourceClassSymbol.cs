using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;
using Draco.Compiler.Internal.Symbols.Metadata;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Represents a class from source code.
/// </summary>
internal sealed class SourceClassSymbol : TypeSymbol
{
    // TODO: Defined members

    public override string Name => this.DeclaringSyntax.Name.Text;

    public override Api.Semantics.Visibility Visibility =>
        GetVisibilityFromTokenKind(this.DeclaringSyntax.VisibilityModifier?.Kind);

    public override ImmutableArray<TypeParameterSymbol> GenericParameters =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, this.BuildGenericParameters);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override bool IsValueType => this.DeclaringSyntax.ValueModifier is not null;

    public override Symbol ContainingSymbol { get; }

    public override ClassDeclarationSyntax DeclaringSyntax => this.declaration.Syntax;

    public override SymbolDocumentation Documentation => InterlockedUtils.InitializeNull(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    private readonly ClassDeclaration declaration;

    public SourceClassSymbol(Symbol containingSymbol, ClassDeclaration declaration)
    {
        this.ContainingSymbol = containingSymbol;
        this.declaration = declaration;
    }

    public override string ToString() => this.DeclaringSyntax.Name.Text;

    private ImmutableArray<TypeParameterSymbol> BuildGenericParameters()
    {
        var genericParams = this.DeclaringSyntax.Generics;
        if (genericParams is null) return ImmutableArray<TypeParameterSymbol>.Empty;

        return genericParams.Parameters.Values
            .Select(syntax => new SourceTypeParameterSymbol(this, syntax))
            .Cast<TypeParameterSymbol>()
            .ToImmutableArray();
    }

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
