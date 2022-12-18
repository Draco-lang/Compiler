using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.DracoIr;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates Draco IR from the <see cref="Ast"/>.
/// </summary>
internal sealed class DracoIrCodegen : AstVisitorBase<Value?>
{
    private readonly AssemblyBuilder assemblyBuilder;
    private ProcBuilder procBuilder = null!;

    public DracoIrCodegen(AssemblyBuilder builder)
    {
        this.assemblyBuilder = builder;
    }

    public override Value VisitFuncDecl(Ast.Decl.Func node)
    {
        throw new NotImplementedException();
    }
}
