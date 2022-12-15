using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.EndToEnd;

// https://github.com/Draco-lang/Compiler/issues/139
public sealed class Issue139Tests
{
    [InlineData("""
        func main() {
            var msg = "Hello! \{}";
        }
        """)]
    [InlineData("""
        func main() {
            var i = i + 
        }
        """)]
    [InlineData("""
        func main() {
            return;
        }
        """)]
    [InlineData("""
        func main(): {}
        """)]
    [Theory]
    public void DoesNotCrash(string source)
    {
        var parseTree = ParseTree.Parse(source);
        var compilation = Compilation.Create(parseTree);
        _ = compilation.GetDiagnostics();
    }
}
