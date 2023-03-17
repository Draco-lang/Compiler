using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Diagnostics;
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
        // TODO: These are not exposed
        var constraints = new ConstraintBag();
        var diagnostics = new DiagnosticBag();
        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax);

        if (this.DeclarationSyntax.Type is not null)
        {
            // var x: T;
            // Type is present
            // TODO: Check value type if present?
            throw new System.NotImplementedException();
        }
        else if (this.DeclarationSyntax.Value is not null)
        {
            // var x = value;
            // Infer from value type
            // TODO
            throw new System.NotImplementedException();
        }
        else
        {
            // A global without a type or value, error
            diagnostics.Add(Diagnostic.Create(
                template: TypeCheckingErrors.CouldNotInferType,
                // TODO: Ugly location API
                location: new Internal.Diagnostics.Location.TreeReference(this.DeclarationSyntax),
                formatArgs: this.Name));
            return ErrorType.Instance;
        }
    }
}
