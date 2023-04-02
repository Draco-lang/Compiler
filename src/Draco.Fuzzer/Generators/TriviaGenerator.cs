using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Fuzzer.Generators;

/// <summary>
/// Generates a random valid syntax trivia.
/// </summary>
internal sealed class TriviaGenerator : IGenerator<SyntaxTrivia>
{
    // TODO
    public string ToString(SyntaxTrivia value) => throw new NotImplementedException();

    // TODO
    public SyntaxTrivia NextEpoch() => throw new NotImplementedException();

    // TODO
    public SyntaxTrivia NextMutation() => throw new NotImplementedException();
}
