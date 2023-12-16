using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Lowering;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Generates IR code for a class.
/// </summary>
internal sealed class ClassCodegen : SymbolVisitor
{
    private Compilation Compilation => this.moduleCodegen.Compilation;
    private bool EmitSequencePoints => this.moduleCodegen.EmitSequencePoints;

    private readonly ModuleCodegen moduleCodegen;
    private readonly Class @class;

    public ClassCodegen(ModuleCodegen moduleCodegen, Class @class)
    {
        this.moduleCodegen = moduleCodegen;
        this.@class = @class;
    }

    // TODO: Copypasta from ModuleCodegen
    public override void VisitFunction(FunctionSymbol functionSymbol)
    {
        if (functionSymbol.Body is null) return;

        // Add procedure
        var procedure = this.@class.DefineProcedure(functionSymbol);

        // Create the body
        var body = this.RewriteBody(functionSymbol.Body);
        // Yank out potential local functions and closures
        var (bodyWithoutLocalFunctions, localFunctions) = ClosureRewriter.Rewrite(body);
        // Compile it
        var bodyCodegen = new FunctionBodyCodegen(this.Compilation, procedure);
        bodyWithoutLocalFunctions.Accept(bodyCodegen);

        // Compile the local functions
        foreach (var localFunc in localFunctions) this.VisitFunction(localFunc);
    }

    public override void VisitField(FieldSymbol fieldSymbol)
    {
        // TODO
        throw new NotImplementedException();
    }

    // TODO: Copypasta from ModuleCodegen
    private BoundNode RewriteBody(BoundNode body)
    {
        // If needed, inject sequence points
        if (this.EmitSequencePoints) body = SequencePointInjector.Inject(body);
        // Desugar it
        return body.Accept(new LocalRewriter(this.Compilation));
    }
}
