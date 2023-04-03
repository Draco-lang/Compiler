using System;
using System.Collections;
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

    /// <summary>
    /// The range this sequence point corresponds to, if any.
    /// </summary>
    public SyntaxRange? Range { get; }

    public SequencePoint(SyntaxNode? syntax, SyntaxRange? range)
    {
        this.Syntax = syntax;
        this.Range = range;
    }

    public override SequencePoint Clone() => new(this.Syntax, this.Range);

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append("@sequence point");
        var range = this.Range ?? this.Syntax?.Range;
        if (range is not null)
        {
            var start = range.Value.Start;
            var end = range.Value.End;
            result.Append($" [{start.Line}:{start.Column}-{end.Line}:{end.Column}]");
        }
        if (this.Syntax is not null) result.Append($" ({this.Syntax})");
        return result.ToString();
    }
}
