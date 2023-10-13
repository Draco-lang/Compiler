using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Lowering;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Generates IR code on module-level.
/// </summary>
internal sealed class ModuleCodegen : SymbolVisitor
{
    private readonly FunctionBodyCodegen globalInitializer;
    private readonly Compilation compilation;
    private readonly Module module;
    private readonly bool emitSequencePoints;

    public ModuleCodegen(Compilation compilation, Module module, bool emitSequencePoints)
    {
        this.compilation = compilation;
        this.module = module;
        this.globalInitializer = new(this.compilation, module.GlobalInitializer);
        this.emitSequencePoints = emitSequencePoints;
    }

    private void Complete()
    {
        // Complete anything that needs completion
        // The global initializer for example is missing a return
        this.globalInitializer.Write(Ret(default(Void)));
    }

    public override void VisitGlobal(GlobalSymbol globalSymbol)
    {
        if (globalSymbol is not SourceGlobalSymbol sourceGlobal) return;

        this.module.DefineGlobal(sourceGlobal);

        // If there's a value, compile it
        if (sourceGlobal.Value is not null)
        {
            var body = this.RewriteBody(sourceGlobal.Value);
            // Yank out potential local functions and closures
            var (bodyWithoutLocalFunctions, localFunctions) = ClosureRewriter.Rewrite(body);
            // Compile it
            var value = bodyWithoutLocalFunctions.Accept(this.globalInitializer);
            // Store it
            value = this.globalInitializer.BoxIfNeeded(sourceGlobal.Type, value);
            this.globalInitializer.Write(Store(sourceGlobal, value));

            // Compile the local functions
            foreach (var localFunc in localFunctions) this.VisitFunction(localFunc);
        }
    }

    public override void VisitFunction(FunctionSymbol functionSymbol)
    {
        if (functionSymbol is not SourceFunctionSymbol sourceFunction) return;

        // Add procedure
        var procedure = this.module.DefineProcedure(functionSymbol);

        // Create the body
        var body = this.RewriteBody(sourceFunction.Body);
        // Yank out potential local functions and closures
        var (bodyWithoutLocalFunctions, localFunctions) = ClosureRewriter.Rewrite(body);
        // Compile it
        var bodyCodegen = new FunctionBodyCodegen(this.compilation, procedure);
        bodyWithoutLocalFunctions.Accept(bodyCodegen);

        // Compile the local functions
        foreach (var localFunc in localFunctions) this.VisitFunction(localFunc);
    }

    public override void VisitModule(ModuleSymbol moduleSymbol)
    {
        foreach (var subModuleSymbol in moduleSymbol.Members.OfType<ModuleSymbol>())
        {
            var module = this.module.DefineModule(subModuleSymbol);
            var moduleCodegen = new ModuleCodegen(this.compilation, module, this.emitSequencePoints);
            subModuleSymbol.Accept(moduleCodegen);
        }

        foreach (var member in moduleSymbol.Members.Where(x => x is not ModuleSymbol))
        {
            member.Accept(this);
        }
        this.Complete();
    }

    private BoundNode RewriteBody(BoundNode body)
    {
        // If needed, inject sequence points
        if (this.emitSequencePoints) body = SequencePointInjector.Inject(body);
        // Desugar it
        return body.Accept(new LocalRewriter(this.compilation));
    }
}
