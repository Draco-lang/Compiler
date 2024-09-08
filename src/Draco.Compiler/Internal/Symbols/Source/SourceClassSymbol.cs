using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Declarations;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A generic class defined in-source.
/// </summary>
internal sealed class SourceClassSymbol(
    Symbol containingSymbol,
    ClassDeclarationSyntax syntax) : TypeSymbol, ISourceSymbol
{
    public override ImmutableArray<TypeParameterSymbol> GenericParameters => this.BindGenericParametersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public SourceClassSymbol(Symbol containingSymbol, ClassDeclaration declaration) : this(containingSymbol, declaration.Syntax)
    {
    }

    public override Symbol? ContainingSymbol { get; } = containingSymbol;
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override ClassDeclarationSyntax DeclaringSyntax => syntax;

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

    private ImmutableArray<object> BindMembersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.members, () => this.BindMembers(binderProvider));

    private ImmutableArray<IMemberSymbol> BindMembers(IBinderProvider binder)
    {
        if(this.DeclaringSyntax.Body is EmptyClassBodySyntax) return [];

        var bodyClass = this.DeclaringSyntax.Body as BlockClassBodySyntax;
        Debug.Assert(bodyClass is not null);
        var declarationsSyntaxes = bodyClass.Declarations.ToList();
        var members = ImmutableArray.CreateBuilder<IMemberSymbol>(declarationsSyntaxes.Count);
        foreach (var declarationSyntax in declarationsSyntaxes)
        {
            var member = this.BindDeclaration(declarationSyntax);
            members.Add(member);
        }

    }

    private IMemberSymbol BindDeclaration(IBinderProvider binder, DeclarationSyntax declarationSyntax)
    {
        switch (declarationSyntax)
        {
            case FunctionDeclarationSyntax functionSyntax:
                
        }
    }

    public override string ToString() => $"{this.Name}{this.GenericsToString()}";
}
