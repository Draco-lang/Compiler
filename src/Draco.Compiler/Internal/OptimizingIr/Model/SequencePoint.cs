using System.Text;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A pseudo-instruction for representing sequence points.
/// </summary>
internal sealed class SequencePoint(SyntaxRange? range) : InstructionBase
{
    public override string InstructionKeyword => "@sequence point";

    /// <summary>
    /// The range this sequence point corresponds to, if any.
    /// </summary>
    public SyntaxRange? Range { get; } = range;

    public override SequencePoint Clone() => new(this.Range);

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(this.InstructionKeyword);
        if (this.Range is not null)
        {
            var start = this.Range.Value.Start;
            var end = this.Range.Value.End;
            result.Append($" [{start.Line}:{start.Column}-{end.Line}:{end.Column}]");
        }
        return result.ToString();
    }
}
