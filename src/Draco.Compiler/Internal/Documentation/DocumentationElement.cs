namespace Draco.Compiler.Internal.Documentation;

internal abstract record class DocumentationElement
{
    public abstract string ToMarkdown();

    public abstract string ToXml();
}

internal sealed record class RawTextDocumentationElement(string RawText) : DocumentationElement
{
    public override string ToMarkdown() => this.RawText;

    public override string ToXml() => this.RawText;
}
