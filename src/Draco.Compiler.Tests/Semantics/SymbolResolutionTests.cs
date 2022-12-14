using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;

namespace Draco.Compiler.Tests.Semantics;

public sealed class SymbolResolutionTests
{
    [Fact]
    public void BasicScopeTree()
    {
        // func foo() {
        //     {
        //         { }
        //     }
        //     {
        //         { }
        //         { }
        //     }
        // }

        //var tree = CompilationUnit();
    }
}
