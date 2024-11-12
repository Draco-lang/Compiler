using Draco.Compiler.Api;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Lowering;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Syntax;
using Draco.Compiler.Internal.Symbols.Synthetized.AutoProperty;

namespace Draco.Compiler.Internal.OptimizingIr.Codegen;

/// <summary>
/// Generates IR code for a class.
/// </summary>
internal sealed class ClassCodegen(ModuleCodegen moduleCodegen, Class @class) : SymbolVisitor
{
    private Compilation Compilation => moduleCodegen.Compilation;
    private bool EmitSequencePoints => moduleCodegen.EmitSequencePoints;

    // Copypasta from ModuleCodegen
    public override void VisitFunction(FunctionSymbol functionSymbol)
    {
        if (functionSymbol.Body is null) return;

        // Add procedure
        var procedure = @class.DefineProcedure(functionSymbol);

        // Create the body
        var body = this.RewriteBody(functionSymbol.Body);
        // Yank out potential local functions and closures
        var (bodyWithoutLocalFunctions, localFunctions) = ClosureRewriter.Rewrite(body);
        // Compile it
        var bodyCodegen = new LocalCodegen(this.Compilation, procedure);
        bodyWithoutLocalFunctions.Accept(bodyCodegen);

        // Compile the local functions
        foreach (var localFunc in localFunctions) this.VisitFunction(localFunc);
    }

    public override void VisitField(FieldSymbol fieldSymbol)
    {
        if (fieldSymbol is not SyntaxFieldSymbol and not AutoPropertyBackingFieldSymbol) return;

        // TODO: Initializer value
        @class.DefineField(fieldSymbol);
    }

    public override void VisitProperty(PropertySymbol propertySymbol)
    {
        // TODO: Not flexible, won't work for non-auto props
        if (propertySymbol is not SyntaxAutoPropertySymbol) return;

        @class.DefineProperty(propertySymbol);

        // TODO: Initializer value
    }

    private BoundNode RewriteBody(BoundNode body)
    {
        // If needed, inject sequence points
        if (body.Syntax is not null && this.EmitSequencePoints) body = SequencePointInjector.Inject(body);
        // Desugar it
        return body.Accept(new LocalRewriter(this.Compilation));
    }
}
