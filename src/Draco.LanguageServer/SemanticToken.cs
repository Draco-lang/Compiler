using System.Collections.Immutable;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer;

internal record struct SemanticToken(SemanticTokenType? Type, ImmutableList<SemanticTokenModifier> Modifiers, Compiler.Api.Syntax.Range Range)
{
    public SemanticToken(SemanticTokenType? Type, SemanticTokenModifier Modifier, Compiler.Api.Syntax.Range Range)
        : this(Type, ImmutableList.Create(Modifier), Range) { }
}
