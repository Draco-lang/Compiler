using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Lowering;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Codegen;


/// <summary>
/// Generates IR code for a class.
/// </summary>
internal sealed class ClassCodegen(ModuleCodegen moduleCodegen, Type @class) : SymbolVisitor
{
    private Compilation Compilation => moduleCodegen.Compilation;
    private bool EmitSequencePoints => moduleCodegen.EmitSequencePoints;

    // Copypasta from ModuleCodegen
    public override void VisitFunction(FunctionSymbol functionSymbol)
    {
        if (functionSymbol.Body is null) return;

        // Add procedure
        var procedure = @class.DefineMethod(functionSymbol);

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
        // No-op, the Class model reads it up from the symbol
    }

    private BoundNode RewriteBody(BoundNode body)
    {
        // If needed, inject sequence points
        if (body.Syntax is not null && this.EmitSequencePoints) body = SequencePointInjector.Inject(body);
        // Desugar it
        return body.Accept(new LocalRewriter(this.Compilation));
    }
}
