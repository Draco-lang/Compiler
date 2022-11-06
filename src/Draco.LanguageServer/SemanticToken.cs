using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer;
internal class SemanticToken
{
    public SemanticTokenType? Type;
    public List<SemanticTokenModifier> Modifiers = new List<SemanticTokenModifier>();
    public Compiler.Api.Syntax.ParseTree Token;
    public SemanticToken(SemanticTokenType? type, List<SemanticTokenModifier> modifiers, Compiler.Api.Syntax.ParseTree token)
    {
        this.Type = type;
        this.Modifiers = modifiers;
        this.Token = token;
    }

    public SemanticToken(SemanticTokenType? type, SemanticTokenModifier modifier, Compiler.Api.Syntax.ParseTree token)
    {
        this.Type = type;
        this.Modifiers.Add(modifier);
        this.Token = token;
    }
}
