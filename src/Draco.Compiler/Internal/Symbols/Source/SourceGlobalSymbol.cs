using System.Diagnostics;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Source;

internal sealed class SourceGlobalSymbol : GlobalSymbol, ISourceSymbol
{
    public override Type Type => this.type ??= this.BuildType();
    private Type? type;

    public override bool IsMutable => this.declaration.Syntax.Keyword.Kind == TokenKind.KeywordVar;
    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.declaration.Name;

    public VariableDeclarationSyntax DeclarationSyntax => this.declaration.Syntax;
    SyntaxNode ISourceSymbol.DeclarationSyntax => this.DeclarationSyntax;

    public BoundExpression? Value => this.DeclarationSyntax.Value is null
        ? null
        : (this.value ??= this.BuildValue());
    private BoundExpression? value;

    public override string Documentation => this.DeclarationSyntax.Documentation;

    private readonly GlobalDeclaration declaration;

    public SourceGlobalSymbol(Symbol? containingSymbol, GlobalDeclaration declaration)
    {
        this.ContainingSymbol = containingSymbol;
        this.declaration = declaration;
    }

    public override ISymbol ToApiSymbol() => new Api.Semantics.GlobalSymbol(this);

    private Type BuildType()
    {
        Debug.Assert(this.DeclaringCompilation is not null);
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;

        if (this.DeclarationSyntax.Type is not null)
        {
            // var x: T;
            // Type is present
            var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax.Type.Type);
            var typeSymbol = (TypeSymbol)binder.BindType(this.DeclarationSyntax.Type.Type, diagnostics);
            var type = typeSymbol.Type;
            // Check for value
            if (this.DeclarationSyntax.Value is not null)
            {
                var value = this.Value!;
                // TODO: In reality we should check assignability, but that's
                // not exposed, only in constraint bag
                if (!ReferenceEquals(type, value.TypeRequired))
                {
                    diagnostics.Add(Diagnostic.Create(
                        template: TypeCheckingErrors.TypeMismatch,
                        location: this.DeclarationSyntax.Value.Value.Location,
                        formatArgs: new[] { type, value.TypeRequired }));
                }
            }
            return type;
        }
        else if (this.DeclarationSyntax.Value is not null)
        {
            // var x = value;
            // Infer from value type
            return this.Value!.TypeRequired;
        }
        else
        {
            // A global without a type or value, error
            diagnostics.Add(Diagnostic.Create(
                template: TypeCheckingErrors.CouldNotInferType,
                location: this.DeclarationSyntax.Location,
                formatArgs: this.Name));
            return ErrorType.Instance;
        }
    }

    private BoundExpression BuildValue()
    {
        Debug.Assert(this.DeclaringCompilation is not null);

        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax.Value!.Value);
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;
        return binder.BindGlobalValue(this.DeclarationSyntax.Value!.Value, diagnostics);
    }
}
