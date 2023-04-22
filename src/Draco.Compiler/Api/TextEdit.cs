using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api;

/// <summary>
/// Represents an edit in source text.
/// </summary>
/// <param name="Range">The range of the text that will be replaced by <paramref name="Text"/>.</param>
/// <param name="Text">The text that should be placed into the source document.</param>
public record class TextEdit(SyntaxRange Range, string Text);
