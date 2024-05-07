using System.Text;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal sealed class LineStateMachine(string indentation)
{
    private readonly StringBuilder sb = new();
    private bool previousIsWhitespace = true;
    private bool prevTokenNeedRightPad = false;
    private bool shouldIndent = true;

    public int LineWidth { get; set; } = indentation.Length;

    public void AddToken(TokenMetadata metadata, FormatterSettings settings, bool endOfInput)
    {
        this.HandleLeadingComments(metadata, settings, endOfInput);
        if (this.shouldIndent)
        {
            this.shouldIndent = false;
            if (metadata.Kind.HasFlag(WhitespaceBehavior.RemoveOneIndentation))
            {
                this.Append(indentation.Remove(settings.Indentation.Length));
            }
            else
            {
                this.Append(indentation);
            }
        }

        var requestedLeftPad = this.prevTokenNeedRightPad || metadata.Kind.HasFlag(WhitespaceBehavior.PadLeft);
        var haveWhitespace = (metadata.Kind.HasFlag(WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken) || this.previousIsWhitespace);
        var shouldLeftPad = (requestedLeftPad && !haveWhitespace);

        if (shouldLeftPad)
        {
            this.Append(" ");
        }
        this.Append(metadata.Text);

        this.prevTokenNeedRightPad = metadata.Kind.HasFlag(WhitespaceBehavior.PadRight);
        this.previousIsWhitespace = metadata.Kind.HasFlag(WhitespaceBehavior.BehaveAsWhiteSpaceForNextToken);
    }

    private void HandleLeadingComments(TokenMetadata metadata, FormatterSettings settings, bool endOfInput)
    {
        if (metadata.LeadingTrivia == null) return;

        foreach (var trivia in metadata.LeadingTrivia)
        {
            if (!string.IsNullOrWhiteSpace(trivia)) this.sb.Append(indentation);
            this.sb.Append(trivia);
            if (!endOfInput)
            {
                this.sb.Append(settings.Newline);
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
        this.sb.Append(indentation);
        this.LineWidth = indentation.Length;
    }


    public override string ToString() => this.sb.ToString();
}
