using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
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

    public override void VisitFunction(FunctionSymbol functionSymbol)
    {
        // TODO
        throw new NotImplementedException();
    }

    public override void VisitField(FieldSymbol fieldSymbol)
    {
        // TODO
        throw new NotImplementedException();
    }
}
