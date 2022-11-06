using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer;

internal class SemanticToken
{
    public SemanticTokenType? Type { get; private set; }
    public List<SemanticTokenModifier> Modifiers { get; private set; } = new List<SemanticTokenModifier>();
    public Compiler.Api.Syntax.Range Range { get; private set; }
    public SemanticToken(SemanticTokenType? type, List<SemanticTokenModifier> modifiers, Compiler.Api.Syntax.Range range)
    {
        this.Type = type;
        this.Modifiers = modifiers;
        this.Range = range;
    }

    public SemanticToken(SemanticTokenType? type, SemanticTokenModifier modifier, Compiler.Api.Syntax.Range range)
    {
        this.Type = type;
        this.Modifiers.Add(modifier);
        this.Range = range;
    }
}
