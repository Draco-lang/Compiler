using System;
using System.Collections.Generic;
using Draco.Compiler.Api.Syntax;
using Draco.Fuzzing;

namespace Draco.Compiler.Fuzzer;

internal sealed class SyntaxTreeInputMutator : IInputMutator<SyntaxTree>
{
    public IEnumerable<SyntaxTree> Mutate(Random random, SyntaxTree input) => throw new System.NotImplementedException();
}
