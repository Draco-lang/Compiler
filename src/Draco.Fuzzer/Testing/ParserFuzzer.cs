using Draco.Compiler.Internal.Syntax;
using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer.Testing;

internal sealed class ParserFuzzer : ComponentFuzzer<TokenArray>
{
    public ParserFuzzer(IInputGenerator<TokenArray> generator) : base(generator) { }

    public override void RunEpoch(TokenArray input)
    {
        // We just care about the parsing into compilation unit part
        new Parser(TokenSource.From(input.Memory), new SyntaxDiagnosticTable()).ParseCompilationUnit();
    }

    public override void RunMutation() => throw new NotImplementedException();
}
