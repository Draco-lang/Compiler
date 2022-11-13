using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using CompilerApi = Draco.Compiler.Api;
using LspModels = OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer;

/// <summary>
/// Does translation between Compiler API and LSP types.
/// </summary>
internal static class Translator
{
    public static LspModels.Diagnostic ToLsp(CompilerApi.Diagnostics.Diagnostic diag) => new()
    {
        Message = diag.Message,
        // TODO: Not necessarily an error
        Severity = LspModels.DiagnosticSeverity.Error,
        // TODO: Is there a no-range option?
        Range = ToLsp(diag.Location?.Range) ?? new(),
    };

    public static LspModels.Range? ToLsp(CompilerApi.Syntax.Range? range) => range is null
        ? null
        : ToLsp(range.Value);

    public static LspModels.Range ToLsp(CompilerApi.Syntax.Range range) =>
        new(ToLsp(range.Start), ToLsp(range.End));

    public static LspModels.Position ToLsp(CompilerApi.Syntax.Position position) =>
        new(line: position.Line, character: position.Column);

    public static SemanticToken? ToLsp(CompilerApi.Syntax.ParseTree.Token token) => token.Type switch
    {
        TokenType.LineStringStart or TokenType.LineStringEnd or TokenType.MultiLineStringStart or TokenType.MultiLineStringEnd or TokenType.LiteralCharacter =>
        new SemanticToken(SemanticTokenType.String, SemanticTokenModifier.Defaults.ToImmutableList(), token.Range),
        _ => null,
    };
}
