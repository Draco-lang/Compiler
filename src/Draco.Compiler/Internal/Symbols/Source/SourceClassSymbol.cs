using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Declarations;
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
    public override ImmutableArray<TypeParameterSymbol> GenericParameters => this.BindGenericParametersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override IEnumerable<Symbol> DefinedMembers => this.BindMembersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<Symbol> definedMembers;

    public SourceClassSymbol(Symbol containingSymbol, ClassDeclaration declaration) : this(containingSymbol, declaration.Syntax)
    {
    }

    public override Symbol ContainingSymbol { get; } = containingSymbol;
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

    private ImmutableArray<Symbol> BindMembersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.definedMembers, () => this.BindMembers(binderProvider));

    private ImmutableArray<Symbol> BindMembers(IBinderProvider binder)
    {
        if (this.DeclaringSyntax.Body is EmptyClassBodySyntax) return [new DefaultConstructorSymbol(this)];

        var bodyClass = this.DeclaringSyntax.Body as BlockClassBodySyntax;
        Debug.Assert(bodyClass is not null);
        var declarationsSyntaxes = bodyClass.Declarations.ToList();
        var members = ImmutableArray.CreateBuilder<Symbol>(declarationsSyntaxes.Count + 1);
        members.Add(new DefaultConstructorSymbol(this));
        foreach (var declarationSyntax in declarationsSyntaxes)
        {
            var member = this.BindDeclaration(binder, declarationSyntax);
            members.Add(member);
        }
        return members.ToImmutable();
    }

    private Symbol BindDeclaration(IBinderProvider binder, DeclarationSyntax declarationSyntax)
    {
        switch (declarationSyntax)
        {
        case FunctionDeclarationSyntax functionSyntax:
            return new SourceFunctionSymbol(this, functionSyntax);
        case VariableDeclarationSyntax fieldSyntax:
            if (fieldSyntax.FieldModifier is null) throw new NotImplementedException();
            return new SourceFieldSymbol(this, fieldSyntax);
        default:
            throw new NotImplementedException(); // TODO implement this
        }
    }

    public override string ToString() => $"{this.Name}{this.GenericsToString()}";
}
