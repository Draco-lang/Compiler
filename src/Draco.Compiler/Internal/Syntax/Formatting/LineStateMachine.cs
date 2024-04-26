using System.Text;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal class LineStateMachine
{
    private readonly StringBuilder sb = new();
    private readonly string indentation;
    private bool previousIsWhitespace = true;
    public LineStateMachine(string indentation)
    {
        this.sb.Append(indentation);
        this.LineWidth = indentation.Length;
        this.indentation = indentation;
    }

    public int LineWidth { get; set; }
    public void AddToken(TokenDecoration decoration, Api.Syntax.SyntaxToken token)
    {
        if (decoration.Kind.HasFlag(FormattingTokenKind.PadLeft) && !this.previousIsWhitespace)
        {
            this.sb.Append(' ');
            this.LineWidth++;
        }
        var text = decoration.TokenOverride ?? token.Text;
        this.sb.Append(text);
        if (decoration.Kind.HasFlag(FormattingTokenKind.PadRight))
        {
            this.sb.Append(' ');
            this.LineWidth++;
        }
        this.previousIsWhitespace = decoration.Kind.HasFlag(FormattingTokenKind.TreatAsWhitespace) || decoration.Kind.HasFlag(FormattingTokenKind.PadRight);
        this.LineWidth += text.Length;
    }

    public void Reset()
    {
        this.sb.Clear();
        this.sb.Append(this.indentation);
        this.LineWidth = this.indentation.Length;
    }


    public override string ToString() => this.sb.ToString();
}
