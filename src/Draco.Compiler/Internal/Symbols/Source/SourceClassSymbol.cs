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
        if (this.IsValueType)
        {
            result.Add(this.DeclaringCompilation!.WellKnownTypes.SystemValueType);
        }
        else
        {
            result.Add(this.DeclaringCompilation!.WellKnownTypes.SystemObject);
        }
        // Done
        return result.ToImmutable();
    }

    // TODO: Check for illegal shadowing
    private ImmutableArray<Symbol> BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        if (this.DeclaringSyntax.PrimaryConstructor is null)
        {
            // We only synthetize a default ctor, if this is a reference type
            if (!this.IsValueType)
            {
                // TODO: Check for secondary constructors
                // If there is no constructor, add a default one
                result.Add(new DefaultConstructorSymbol(this));
            }
        }
        else
        {
            // We have a primary constructor, add it
            result.Add(new SourcePrimaryConstructorSymbol(this, this.DeclaringSyntax.PrimaryConstructor));

            // Also check for fields/props
            foreach (var param in this.DeclaringSyntax.PrimaryConstructor.ParameterList.Values)
            {
                // Skip non-members
                if (param.MemberModifiers is null) continue;

                if (param.MemberModifiers.FieldModifier is null)
                {
                    // Property
                    var prop = new SourceAutoPropertySymbol(this, param);
                    // Add property, getter, setter and backing field
                    result.Add(prop);
                    result.Add(prop.Getter);
                    if (prop.Setter is not null) result.Add(prop.Setter);
                    result.Add(prop.BackingField);
                }
                else
                {
                    // Add field
                    result.Add(new SourceFieldSymbol(this, param));
                }
            }
        }

        // Done
        return result.ToImmutable();
    }

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
