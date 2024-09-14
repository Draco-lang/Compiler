using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;
internal class SourcePropertySymbol(VariableDeclarationSyntax syntax) : PropertySymbol, ISourceSymbol, IMemberSymbol
{
    public override VariableDeclarationSyntax DeclaringSyntax { get; } = syntax;

    public override FunctionSymbol? Getter => this.BindGetterIfNeeded(this.DeclaringCompilation!);

    private FunctionSymbol? getter;

    public override FunctionSymbol? Setter => InterlockedUtils.InitializeMaybeNull(ref this.setter, this.BuildSetter);
    private FunctionSymbol? setter;

    public override TypeSymbol Type => this.BindTypeIfNeeded(this.DeclaringCompilation!);
    private TypeSymbol? type;


    public override bool IsIndexer => throw new NotImplementedException("todo");

    public override bool IsStatic { get; } = syntax.StaticKeyword is not null;

    private FunctionSymbol? BindGetterIfNeeded(Compilation compilation) =>
        InterlockedUtils.InitializeMaybeNull(ref this.getter, () => this.BindGetter(compilation));

    private FunctionSymbol? BindGetter(Compilation compilation)
    {
        if (this.DeclaringSyntax.Value is null) return null;
        throw new NotImplementedException("todo");
    }

    private TypeSymbol BindTypeIfNeeded(IBinderProvider binderProvider) =>
        LazyInitializer.EnsureInitialized(ref this.type, () => this.BindType(binderProvider));

    private TypeSymbol BindType(IBinderProvider binderProvider)
    {
        if (this.DeclaringSyntax.Type is null) throw new NotImplementedException(); // TODO: what should I do when the type is missing? Do we allow inference here ?

        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindTypeToTypeSymbol(this.DeclaringSyntax.Type.Type, binderProvider.DiagnosticBag);
    }
}
