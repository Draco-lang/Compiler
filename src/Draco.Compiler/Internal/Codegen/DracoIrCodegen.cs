using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.DracoIr;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.Symbols;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates Draco IR from the <see cref="Ast"/>.
/// </summary>
internal sealed class DracoIrCodegen : AstVisitorBase<Value>
{
    /// <summary>
    /// Generates IR code in the given <see cref="Assembly"/> for the given <see cref="Ast"/>.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> to generate the IR into.</param>
    /// <param name="ast">The <see cref="Ast"/> to generate IR code for.</param>
    public static void Generate(Assembly assembly, Ast ast)
    {
        var codegen = new DracoIrCodegen(assembly);
        codegen.Visit(ast);
    }

    private readonly Assembly assembly;
    private InstructionWriter writer = null!;

    private readonly Dictionary<ISymbol.IFunction, Procedure> procedures = new();

    private DracoIrCodegen(Assembly assembly)
    {
        this.assembly = assembly;
    }

    private Procedure GetProcedure(ISymbol.IFunction function)
    {
        if (!this.procedures.TryGetValue(function, out var proc))
        {
            proc = new(function.Name);
            this.procedures.Add(function, proc);
        }
        return proc;
    }

    public override Value VisitFuncDecl(Ast.Decl.Func node)
    {
        var oldWriter = this.writer;
        this.writer = this.GetProcedure(node.DeclarationSymbol).Writer();

        // TODO: Parameters
        // TODO: Return type

        this.VisitBlockExpr(node.Body);

        this.writer = oldWriter;
        return this.Default;
    }
}
