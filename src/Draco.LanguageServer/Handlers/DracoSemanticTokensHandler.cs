using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Draco.Compiler.Api.Syntax;
using System.Reflection;

namespace Draco.LanguageServer.Handlers;
public class DracoSemanticTokensHandler : SemanticTokensHandlerBase
{
    private ParseTree? parseTree;

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
        //using var typesEnumerator = this.RotateEnum(SemanticTokenType.Defaults).GetEnumerator();
        //using var modifiersEnumerator = this.RotateEnum(SemanticTokenModifier.Defaults).GetEnumerator();
        // you would normally get this from a common source that is managed by current open editor, current active editor, etc.
        var content = await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(identifier)!, cancellationToken).ConfigureAwait(false);
        this.parseTree = ParseTree.Parse(content);
        var tokens = this.GetTokens(this.parseTree);
        await Task.Yield();
        foreach (var token in tokens)
        {
            builder.Push(token.Element.Range.Start.Line, token.Element.Range.Start.Column, token.Element.Width, token.Type, token.Modifiers);
        }
        //foreach (var (line, text) in content.Split('\n').Select((text, line) => (line, text)))
        //{
        //    var parts = text.TrimEnd().Split(';', ' ', '.', '"', '(', ')');
        //    var index = 0;
        //    foreach (var part in parts)
        //    {
        //        typesEnumerator.MoveNext();
        //        modifiersEnumerator.MoveNext();
        //        if (string.IsNullOrWhiteSpace(part)) continue;
        //        index = text.IndexOf(part, index, StringComparison.Ordinal);
        //        builder.Push(line, index, part.Length, typesEnumerator.Current);
        //    }
        //}
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SemanticTokensDocument(this.RegistrationOptions.Legend));
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
    {
        return new SemanticTokensRegistrationOptions
        {
            DocumentSelector = this.documentSelector,
            Legend = new SemanticTokensLegend
            {
                TokenModifiers = capability.TokenModifiers,
                TokenTypes = capability.TokenTypes
            },
            Full = new SemanticTokensCapabilityRequestFull
            {
                Delta = true
            },
            Range = true
        };
    }

    private class SemanticToken
    {
        public SemanticTokenType Type;
        public List<SemanticTokenModifier> Modifiers = new List<SemanticTokenModifier>();
        public Compiler.Api.Syntax.ParseTree Element;
        public SemanticToken(SemanticTokenType type, List<SemanticTokenModifier> modifiers, Compiler.Api.Syntax.ParseTree element)
        {
            this.Type = type;
            this.Modifiers = modifiers;
            this.Element = element;
        }

        public SemanticToken(SemanticTokenType type, SemanticTokenModifier modifier, Compiler.Api.Syntax.ParseTree element)
        {
            this.Type = type;
            this.Modifiers.Add(modifier);
            this.Element = element;
        }
    }

    private List<SemanticToken> GetTokens(ParseTree tree)
    {
        var result = new List<SemanticToken>();
        if (tree is ParseTree.Token token) result.Add(token.Text switch
        {
            "var" => new SemanticToken(SemanticTokenType.Keyword, SemanticTokenModifier.Declaration, token),
            _ => new SemanticToken(SemanticTokenType.Variable, SemanticTokenModifier.Defaults.ToList(), token),
        });
        foreach (var child in tree.Children)
        {
            result.AddRange(this.GetTokens(child));
        }
        return result;
    }
}
