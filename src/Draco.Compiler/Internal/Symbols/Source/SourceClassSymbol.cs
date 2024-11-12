using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Solver.Constraints;
using Draco.Compiler.Internal.Symbols.Generic;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A generic class defined in-source.
/// </summary>
internal sealed class SourceClassSymbol(
    Symbol containingSymbol,
    ClassDeclarationSyntax syntax) : TypeSymbol, ISourceSymbol
{
    public override Symbol ContainingSymbol { get; } = containingSymbol;
    public override ClassDeclarationSyntax DeclaringSyntax { get; } = syntax;
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override ImmutableArray<TypeParameterSymbol> GenericParameters =>
        this.BindGenericParametersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override IEnumerable<Symbol> DefinedMembers =>
        this.BindMembersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<Symbol> definedMembers;

    public SourceClassSymbol(Symbol containingSymbol, ClassDeclaration declaration)
        : this(containingSymbol, declaration.Syntax)
    {
    }

    public void Bind(IBinderProvider binderProvider)
    {
        this.BindGenericParametersIfNeeded(binderProvider);
        this.BindMembersIfNeeded(binderProvider);
    }

    private ImmutableArray<TypeParameterSymbol> BindGenericParametersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, () => this.BindGenericParameters(binderProvider));

    private ImmutableArray<TypeParameterSymbol> BindGenericParameters(IBinderProvider binderProvider)
    {
        if (this.DeclaringSyntax.Generics is null) return [];

        var genericParamSyntaxes = this.DeclaringSyntax.Generics.Parameters.Values.ToList();
        var genericParams = ImmutableArray.CreateBuilder<TypeParameterSymbol>(genericParamSyntaxes.Count);

        foreach (var genericParamSyntax in genericParamSyntaxes)
        {
            var paramName = genericParamSyntax.Name.Text;
            var usedBefore = genericParams.Any(p => p.Name == paramName);
            if (usedBefore)
            {
                // NOTE: We only report later duplicates, no need to report the first instance
                binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.IllegalShadowing,
                    location: genericParamSyntax.Location,
                    formatArgs: paramName));
            }

            var genericParam = new SourceTypeParameterSymbol(this, genericParamSyntax);
            genericParams.Add(genericParam);
        }
        return genericParams.ToImmutable();
    }

    private ImmutableArray<Symbol> BindMembersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.definedMembers, () => this.BindMembers(binderProvider));

    // TODO: This shadowing check logic is duplicated a few places
    // Along with member construction, maybe factor it out
    private ImmutableArray<Symbol> BindMembers(IBinderProvider binderProvider)
    {
        // If there is no body, we only have a default constructor
        if (this.DeclaringSyntax.Body is EmptyClassBodySyntax) return [new DefaultConstructorSymbol(this)];

        if (this.DeclaringSyntax.Body is BlockClassBodySyntax blockBody)
        {
            var result = ImmutableArray.CreateBuilder<Symbol>();
            // NOTE: For now we always add a default constructor
            result.Add(new DefaultConstructorSymbol(this));
            foreach (var member in blockBody.Declarations.Select(this.BuildMember))
            {
                if (member is null) continue;

                var earlierMember = result.FirstOrDefault(s => s.Name == member.Name);
                result.Add(member);

                // We check for illegal shadowing
                if (earlierMember is null) continue;

                // Overloading is legal
                if (member is FunctionSymbol && earlierMember is FunctionSymbol) continue;

                // Illegal
                var syntax = member.DeclaringSyntax;
                Debug.Assert(syntax is not null);
                binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.IllegalShadowing,
                    location: syntax.Location,
                    formatArgs: member.Name));
            }

            // If this is a generic definition, we generic instantiate the members
            if (this.IsGenericDefinition)
            {
                var genericContext = new GenericContext(this.GenericParameters.ToImmutableDictionary(t => t, t => t as TypeSymbol));
                for (var i = 0; i < result.Count; ++i)
                {
                    result[i] = result[i].GenericInstantiate(this, genericContext);
                }
            }

            // Add additional symbols for each
            var origCount = result.Count;
            for (var i = 0; i < origCount; ++i)
            {
                var member = result[i];
                result.AddRange(member.GetAdditionalSymbols());
            }

            return result.ToImmutable();
        }

        return [];
    }

    private Symbol? BuildMember(DeclarationSyntax syntax) => syntax switch
    {
        FunctionDeclarationSyntax functionSyntax => new SourceFunctionSymbol(this, functionSyntax),
        VariableDeclarationSyntax varSyntax when varSyntax.FieldModifier is null => new SourceAutoPropertySymbol(this, varSyntax),
        VariableDeclarationSyntax varSyntax when varSyntax.FieldModifier is not null => new SourceFieldSymbol(this, varSyntax),
        UnexpectedDeclarationSyntax => null,
        _ => throw new NotImplementedException(),
    };

    public override string ToString() => $"{this.Name}{this.GenericsToString()}";
}
