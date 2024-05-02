using System.Text;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal sealed class LineStateMachine
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
        this.HandleLeadingComments(metadata, settings);

        if (metadata.Kind.HasFlag(WhitespaceBehavior.RemoveOneIndentation))
        {
            this.sb.Remove(0, settings.Indentation.Length);
            this.LineWidth -= settings.Indentation.Length;
        }

        var requestedLeftPad = this.prevTokenNeedRightPad || metadata.Kind.HasFlag(WhitespaceBehavior.PadLeft);
        var haveWhitespace = (metadata.Kind.HasFlag(WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken) || this.previousIsWhitespace);
        var shouldLeftPad = (requestedLeftPad && !haveWhitespace) || this.forceWhiteSpace;

        if (shouldLeftPad)
        {
            this.Append(" ");
        }
        this.Append(metadata.TokenOverride ?? metadata.Token.Text);

        this.forceWhiteSpace = metadata.Kind.HasFlag(WhitespaceBehavior.ForceRightPad);
        this.prevTokenNeedRightPad = metadata.Kind.HasFlag(WhitespaceBehavior.PadRight);
        this.previousIsWhitespace = metadata.Kind.HasFlag(WhitespaceBehavior.BehaveAsWhiteSpaceForNextToken) || metadata.Kind.HasFlag(WhitespaceBehavior.ForceRightPad);
    }

    private void HandleLeadingComments(TokenMetadata metadata, FormatterSettings settings)
    {
        if (metadata.LeadingComments.Count > 0)
        {
            foreach (var comment in metadata.LeadingComments)
            {
                this.sb.Append(comment);
                if (metadata.Token.Kind != Api.Syntax.TokenKind.EndOfInput)
                {
                    this.sb.Append(settings.Newline);
                    this.sb.Append(this.indentation);
                    this.LineWidth = this.indentation.Length;
                }
            }
        }
    }

    private void Append(string text)
    {
        this.sb.Append(text);
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
