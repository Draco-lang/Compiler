using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A pseudo-instruction for representing sequence points.
/// </summary>
internal sealed class SequencePoint : InstructionBase
{
    /// <summary>
    /// The syntax this sequence point corresponds to, if any.
    /// </summary>
    public SyntaxNode? Syntax { get; }

    public SequencePoint(SyntaxNode? syntax)
    {
        this.Syntax = syntax;
    }

    public override SequencePoint Clone() => new(this.Syntax);

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append("@sequence point");
        var range = this.Syntax?.Range;
        if (range is not null)
        {
            var start = range.Value.Start;
            var end = range.Value.End;
            result.Append($" [{start.Line}:{start.Column}-{end.Line}:{end.Column}]");
        }
        return result.ToString();
    }
}
