using System.Text;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal class LineStateMachine
{
    private readonly StringBuilder sb = new();
    private readonly string indentation;
    private bool previousIsWhitespace = true;
    private bool prevTokenNeedRightPad = false;
    public LineStateMachine(string indentation)
    {
        this.sb.Append(indentation);
        this.LineWidth = indentation.Length;
        this.indentation = indentation;
    }

    public int LineWidth { get; set; }
    public void AddToken(TokenDecoration decoration, FormatterSettings settings)
    {
        if (decoration.Kind.HasFlag(FormattingTokenKind.RemoveOneIndentation))
        {
            this.sb.Remove(0, settings.Indentation.Length);
        }
        var shouldLeftPad = (this.prevTokenNeedRightPad || decoration.Kind.HasFlag(FormattingTokenKind.PadLeft))
            && !decoration.Kind.HasFlag(FormattingTokenKind.BehaveAsWhiteSpaceForPreviousToken)
            && !this.previousIsWhitespace;
        if (shouldLeftPad)
        {
            this.previousIsWhitespace = true;
            this.sb.Append(' ');
            this.LineWidth++;
        }
        var text = decoration.TokenOverride ?? decoration.Token.Text;
        this.sb.Append(text);
        this.LineWidth += text.Length;
        if (decoration.Kind.HasFlag(FormattingTokenKind.ForceRightPad))
        {
            this.sb.Append(' ');
            this.LineWidth++;
        }
        this.prevTokenNeedRightPad = decoration.Kind.HasFlag(FormattingTokenKind.PadRight);

        this.previousIsWhitespace = decoration.Kind.HasFlag(FormattingTokenKind.BehaveAsWhiteSpaceForNextToken) | decoration.Kind.HasFlag(FormattingTokenKind.ForceRightPad);
    }

    public void Reset()
    {
        this.sb.Clear();
        this.sb.Append(this.indentation);
        this.LineWidth = this.indentation.Length;
    }


    public override string ToString() => this.sb.ToString();
}
