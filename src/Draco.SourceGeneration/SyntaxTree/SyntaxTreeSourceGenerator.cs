using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.SourceGeneration.SyntaxTree;

[Generator]
public sealed class SyntaxTreeSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) => throw new NotImplementedException();
}
