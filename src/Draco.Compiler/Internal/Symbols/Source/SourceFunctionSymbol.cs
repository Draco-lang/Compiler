using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source function-definition.
/// </summary>
internal sealed class SourceFunctionSymbol : FunctionSymbol, ISourceSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters => this.parameters ??= this.BuildParameters();
    private ImmutableArray<ParameterSymbol>? parameters;

    public override Type ReturnType => this.returnType ??= this.BuildReturnType();
    private Type? returnType;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclarationSyntax.Name.Text;

    /// <summary>
    /// The syntax the symbol was constructed from.
    /// </summary>
    public FunctionDeclarationSyntax DeclarationSyntax { get; }
    SyntaxNode ISourceSymbol.DeclarationSyntax => this.DeclarationSyntax;

    public BoundStatement Body => this.body ??= this.BuildBody();
    private BoundStatement? body;

    public override string Documentation => this.DeclarationSyntax.Documentation;

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclarationSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclarationSyntax = syntax;
    }

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclaration declaration)
        : this(containingSymbol, declaration.Syntax)
    {
    }

    public override ISymbol ToApiSymbol() => new Api.Semantics.FunctionSymbol(this);

    private ImmutableArray<ParameterSymbol> BuildParameters()
    {
        // TODO: We should totally get rid of this
        // Instead, we should make both valid but always make referencing the letter
        // This would also get rid of the DuplicateParameterSymbol
        // On their own they are fine, let's just report an error that this is an illegal shadowing
        // No need for special symbol treatment

        Debug.Assert(this.DeclaringCompilation is not null);
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;

        var parameterSyntaxes = this.DeclarationSyntax.ParameterList.Values.ToList();
        var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

        for (var i = 0; i < parameterSyntaxes.Count; ++i)
        {
            var parameterSyntax = parameterSyntaxes[i];
            var parameterName = parameterSyntax.Name.Text;

            var usedBefore = parameters.Any(p => p.Name == parameterName);

            if (usedBefore)
            {
                // NOTE: We only report later duplicates, no need to report the first instance
                diagnostics.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.IllegalShadowing,
                    new SourceLocation(parameterSyntax),
                    formatArgs: parameterName));
            }

            var parameter = new SourceParameterSymbol(this, parameterSyntax);
            parameters.Add(parameter);
        }

        return parameters.ToImmutable();
    }

    private Type BuildReturnType()
    {
        // If the return type is unspecified, it's assumed to be unit
        if (this.DeclarationSyntax.ReturnType is null) return Intrinsics.Unit;

        // Otherwise, we need to resolve
        Debug.Assert(this.DeclaringCompilation is not null);

        // NOTE: We are using the global diagnostic bag, maybe that's not a good idea here?
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;
        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax);
        var returnTypeSymbol = binder.BindType(this.DeclarationSyntax.ReturnType.Type, diagnostics);
        return ((TypeSymbol)returnTypeSymbol).Type;
    }

    private BoundStatement BuildBody()
    {
        Debug.Assert(this.DeclaringCompilation is not null);
        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax.Body);
        return binder.BindFunctionBody(this.DeclarationSyntax.Body);
    }
}
