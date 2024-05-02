using System.Text;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal class LineStateMachine
{
    private readonly StringBuilder sb = new();
    private readonly string indentation;
    private bool previousIsWhitespace = true;
    private bool prevTokenNeedRightPad = false;
    private bool forceWhiteSpace = false;
    public LineStateMachine(string indentation)
    {
        this.sb.Append(indentation);
        this.LineWidth = indentation.Length;
        this.indentation = indentation;
    }

    public int LineWidth { get; set; }
    public void AddToken(TokenMetadata metadata, FormatterSettings settings)
    {
        if (metadata.LeadingComments.Count > 0)
        {
            foreach (var comment in metadata.LeadingComments)
            {
                this.sb.Append(comment);
                this.LineWidth += comment.Length;
                if (metadata.Token.Kind != Api.Syntax.TokenKind.EndOfInput)
                {
                    this.sb.Append(settings.Newline);
                    this.sb.Append(this.indentation);
                }
            }
        }

        if (metadata.Kind.HasFlag(WhitespaceBehavior.RemoveOneIndentation))
        {
            this.sb.Remove(0, settings.Indentation.Length);
        }

        var shouldLeftPad = (this.prevTokenNeedRightPad || metadata.Kind.HasFlag(WhitespaceBehavior.PadLeft))
            && !metadata.Kind.HasFlag(WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken)
            && !this.previousIsWhitespace;
        shouldLeftPad |= this.forceWhiteSpace;
        if (shouldLeftPad)
        {
            this.previousIsWhitespace = true;
            this.forceWhiteSpace = false;
            this.sb.Append(' ');
            this.LineWidth++;
        }
        var text = metadata.TokenOverride ?? metadata.Token.Text;
        this.sb.Append(text);
        this.LineWidth += text.Length;
        if (metadata.Kind.HasFlag(WhitespaceBehavior.ForceRightPad))
        {
            this.forceWhiteSpace = true;
        }
        this.prevTokenNeedRightPad = metadata.Kind.HasFlag(WhitespaceBehavior.PadRight);

        this.previousIsWhitespace = metadata.Kind.HasFlag(WhitespaceBehavior.BehaveAsWhiteSpaceForNextToken) | metadata.Kind.HasFlag(WhitespaceBehavior.ForceRightPad);
    }

    public void Reset()
    {
        this.sb.Clear();
        this.sb.Append(this.indentation);
        this.LineWidth = this.indentation.Length;
    }


    public override string ToString() => this.sb.ToString();
}
