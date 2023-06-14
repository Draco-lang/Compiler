using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.CodeCompletion;
using Draco.Compiler.Api.CodeFixes;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Lsp.Model;
using Draco.Lsp.Server;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ILanguageServer
{
    public InitializeResult.ServerInfoResult Info => new()
    {
        Name = "Draco Language Server",
        Version = "0.1.0",
    };

    public IList<DocumentFilter> DocumentSelector => new[]
    {
        new DocumentFilter()
        {
            Language = "draco",
            Pattern = "**/*.draco",
        }
    };
    public TextDocumentSyncKind SyncKind => TextDocumentSyncKind.Full;

    private readonly ILanguageClient client;
    private readonly DracoConfigurationRepository configurationRepository;
    private readonly DracoDocumentRepository documentRepository = new();

    private Uri rootUri;
    private Compilation compilation;

    private readonly CompletionService completionService;
    private readonly SignatureService signatureService;
    private readonly CodeFixService codeFixService;

    public DracoLanguageServer(ILanguageClient client)
    {
        this.client = client;
        this.configurationRepository = new(client);
        this.rootUri = default!; // Default value, it will be given correct value on initialization

        // Some empty defaults
        this.compilation = Compilation.Create(
            syntaxTrees: ImmutableArray<SyntaxTree>.Empty);

        this.completionService = new CompletionService();
        this.completionService.AddProvider(new KeywordCompletionProvider());
        this.completionService.AddProvider(new ExpressionCompletionProvider());
        this.completionService.AddProvider(new MemberCompletionProvider());

        this.signatureService = new SignatureService();

        this.codeFixService = new CodeFixService();
        this.codeFixService.AddProvider(new ImportCodeFixProvider());
    }

    private void CreateCompilation()
    {
        var rootPath = this.rootUri.LocalPath;
        var syntaxTrees = Directory.GetFiles(rootPath, "*.draco", SearchOption.AllDirectories)
            .Select(x => SyntaxTree.Parse(SourceText.FromFile(x)))
            .ToImmutableArray();

        this.compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            // NOTE: Temporary until we solve MSBuild communication
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: rootPath);
    }

    private SyntaxTree? GetSyntaxTree(DocumentUri documentUri)
    {
        var uri = documentUri.ToUri();
        return this.compilation.SyntaxTrees.FirstOrDefault(t => t.SourceText.Path == uri);
    }

    public void Dispose() { }

    public Task InitializeAsync(InitializeParams param)
    {
        if (param.WorkspaceFolders is null || param.WorkspaceFolders.Count == 0) return Task.CompletedTask;
        this.rootUri = param.WorkspaceFolders[0].Uri;
        this.CreateCompilation();
        return Task.CompletedTask;
    }

    public async Task InitializedAsync(InitializedParams param)
    {
        await this.configurationRepository.UpdateConfigurationAsync();
    }

    public Task ShutdownAsync() => Task.CompletedTask;
}
