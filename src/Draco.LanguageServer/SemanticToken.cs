using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer;

internal record struct SemanticToken(SemanticTokenType? Type, ImmutableList<SemanticTokenModifier> Modifiers, Compiler.Api.Syntax.Range Range)
{
    public SemanticToken(SemanticTokenType? Type, SemanticTokenModifier Modifier, Compiler.Api.Syntax.Range Range)
        : this(Type, ImmutableList.Create(Modifier), Range) { }
}
