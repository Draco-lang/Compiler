using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.IO;
using Draco.Compiler.Api.Syntax;

namespace Draco.LanguageServer.Handlers;

internal sealed class DracoSemanticTokensHandler : SemanticTokensHandlerBase
{
    private readonly DracoDocumentRepository repository;

    internal DracoSemanticTokensHandler(DracoDocumentRepository repository)
    {
        this.repository = repository;
    }

    private readonly DocumentSelector documentSelector = new(new DocumentFilter
    {
        Pattern = $"**/*{Constants.DracoSourceExtension}",
    });

    public override async Task<SemanticTokens?> Handle(SemanticTokensParams request, CancellationToken cancellationToken)
    {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override async Task<SemanticTokens?> Handle(SemanticTokensRangeParams request, CancellationToken cancellationToken)
    {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override async Task<SemanticTokensFullOrDelta?> Handle(SemanticTokensDeltaParams request, CancellationToken cancellationToken)
    {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    protected override async Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
    {
        var content = this.repository.GetDocument(identifier.TextDocument.Uri);
        var parseTree = ParseTree.Parse(content);
        var tokens = GetTokens(parseTree);
        foreach (var token in tokens)
        {
            builder.Push(Translator.ToLsp(token.Range), token.Type, token.Modifiers);
        }
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SemanticTokensDocument(this.RegistrationOptions.Legend));
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability,
        ClientCapabilities clientCapabilities) => capability is null
        ? new()
        : new()
        {
            DocumentSelector = this.documentSelector,
            Legend = new SemanticTokensLegend
            {
                TokenModifiers = capability.TokenModifiers,
                TokenTypes = capability.TokenTypes,
            },
            Full = new SemanticTokensCapabilityRequestFull
            {
                Delta = true
            },
            Range = true
        };

    private static IEnumerable<SemanticToken> GetTokens(ParseTree tree) => tree.Tokens
        .Select(t => Translator.ToLsp(t)!).OfType<SemanticToken>();
}
