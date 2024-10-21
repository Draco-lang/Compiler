using System.Linq;
using System.Threading;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols.Syntax;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source function-definition.
/// </summary>
internal sealed class SourceFunctionSymbol(
    Symbol containingSymbol,
    FunctionDeclarationSyntax syntax) : SyntaxFunctionSymbol(containingSymbol, syntax), ISourceSymbol
{
    public override BoundStatement Body => this.BindBodyIfNeeded(this.DeclaringCompilation!);
    private BoundStatement? body;

    public SourceFunctionSymbol(Symbol containingSymbol, FunctionDeclaration declaration)
        : this(containingSymbol, declaration.Syntax)
    {
    }

    public override bool IsStatic => this.ThisArgument == null;

    public override void Bind(IBinderProvider binderProvider)
    {
        this.BindAttributesIfNeeded(binderProvider);
        this.BindGenericParametersIfNeeded(binderProvider);
        this.BindParametersIfNeeded(binderProvider);
        // Force binding of parameters, as the type is lazy too
        foreach (var param in this.Parameters.Cast<SourceParameterSymbol>()) param.Bind(binderProvider);
        this.BindReturnTypeIfNeeded(binderProvider);
        var body = this.BindBodyIfNeeded(binderProvider);

        // Check, if this function collides with any other overloads that are visible from here
        this.CheckForSameParameterOverloads(binderProvider);

        // Flow analysis
        ReturnsOnAllPaths.Analyze(this, binderProvider.DiagnosticBag);
        DefiniteAssignment.Analyze(body, binderProvider.DiagnosticBag);
        ValAssignment.Analyze(this, binderProvider.DiagnosticBag);
    }

    private void CheckForSameParameterOverloads(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this);
        var overloads = binder.LookupValueSymbol(this.Name, this.DeclaringSyntax, DiagnosticBag.Empty);
        // If not found, do nothing
        if (overloads.IsError) return;
        // If this is the same instance, do nothing
        if (ReferenceEquals(overloads, this)) return;
        // Should not happen, but if not overload set, do nothing
        if (overloads is not FunctionGroupSymbol overloadSymbol) return;
        // Check for same parameter types
        foreach (var func in overloadSymbol.Functions)
        {
            if (ReferenceEquals(func, this)) continue;
            if (!HasSameParameterTypes(func, this)) continue;

            // Report
            binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                template: TypeCheckingErrors.IllegalOverloadDefinition,
                location: this.DeclaringSyntax.Location,
                formatArgs: this.Name,
                relatedInformation: func.DeclaringSyntax is null
                    ? []
                    : [DiagnosticRelatedInformation.Create(
                        location: func.DeclaringSyntax.Location,
                        format: "matching definition of {0}",
                        formatArgs: func.Name)]));
        }
    }

    private BoundStatement BindBodyIfNeeded(IBinderProvider binderProvider) =>
        LazyInitializer.EnsureInitialized(ref this.body, () => this.BindBody(binderProvider));

    private BoundStatement BindBody(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax.Body);
        return binder.BindFunction(this, binderProvider.DiagnosticBag);
    }

    private static bool HasSameParameterTypes(FunctionSymbol f1, FunctionSymbol f2)
    {
        if (f1.Parameters.Length != f2.Parameters.Length) return false;

        for (var i = 0; i < f1.Parameters.Length; ++i)
        {
            var p1 = f1.Parameters[i];
            var p2 = f2.Parameters[i];

            if (!SymbolEqualityComparer.SignatureMatch.Equals(p1.Type, p2.Type)) return false;
        }

        return true;
    }
}
