using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Represents a class from source code.
/// </summary>
internal sealed class SourceClassSymbol : TypeSymbol
{
    public override IEnumerable<Symbol> DefinedMembers =>
        InterlockedUtils.InitializeDefault(ref this.definedMembers, this.BuildMembers);
    private ImmutableArray<Symbol> definedMembers;

    public override string Name => this.DeclaringSyntax.Name.Text;

    public override Api.Semantics.Visibility Visibility =>
        GetVisibilityFromTokenKind(this.DeclaringSyntax.VisibilityModifier?.Kind);

    public override ImmutableArray<TypeParameterSymbol> GenericParameters =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, this.BuildGenericParameters);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override ImmutableArray<TypeSymbol> ImmediateBaseTypes =>
        InterlockedUtils.InitializeDefault(ref this.immediateBaseTypes, this.BuildImmediateBaseTypes);
    private ImmutableArray<TypeSymbol> immediateBaseTypes;

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

    private ImmutableArray<TypeSymbol> BuildImmediateBaseTypes()
    {
        var result = ImmutableArray.CreateBuilder<TypeSymbol>();
        // NOTE: For now we always just inherit from object
        result.Add(this.DeclaringCompilation!.WellKnownTypes.SystemObject);
        // Done
        return result.ToImmutable();
    }

    private ImmutableArray<Symbol> BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        // TODO: Check for secondary constructors
        // If there is no constructor, add a default one
        if (this.DeclaringSyntax.PrimaryConstructor is null)
        {
            result.Add(new DefaultConstructorSymbol(this));
        }

        // Done
        return result.ToImmutable();
    }

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
