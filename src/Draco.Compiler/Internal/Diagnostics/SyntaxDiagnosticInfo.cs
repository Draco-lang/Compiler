using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// Represents diagnostic information attached to nodes of the green tree.
///
/// Since the green tree has no access to parents or the tree itself, we need this separate concept for
/// syntax diagnostics. Once the tree is wrapped around in the red tree, we can convert this to diagnostics
/// with locations.
/// </summary>
/// <param name="Info">The diagnostic information.</param>
/// <param name="Offset">The offset relative to the attached node, not including the leading trivia.</param>
/// <param name="Width">The width of the diagnostic area.</param>
internal readonly record struct SyntaxDiagnosticInfo(
    DiagnosticInfo Info,
    int Offset,
    int Width)
{
    /// <summary>
    /// Converts this syntax diagnostic information to a diagnostic with source location.
    /// </summary>
    /// <param name="node">The red node wrapper of the green node this diagnostic was attached to.</param>
    /// <returns>The diagnostic with source location pointing at <paramref name="node"/>.</returns>
    public Diagnostic ToDiagnostic(SyntaxNode node) => Diagnostic.Create(
        template: this.Info.Template,
        location: new SourceLocation(node.Tree, new SourceSpan(
            Start: node.Span.Start + this.Offset,
            Length: this.Width)),
        formatArgs: this.Info.FormatArgs);
}
