using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.Diagnostics;

/// <summary>
/// Represents an in-source location.
/// </summary>
internal sealed class SourceLocation : Location
{
    public override SourceText SourceText { get; }
    public override SyntaxRange? Range { get; }

    public SourceLocation(SyntaxTree syntaxTree, SourceSpan span)
    {
        this.SourceText = syntaxTree.SourceText;
        this.Range = (SyntaxRange?)null ?? throw new NotImplementedException();
    }

    public override string ToString()
    {
        var position = this.Range!.Value.Start;
        return $"at line {position.Line + 1}, character {position.Column + 1}";
    }
}
