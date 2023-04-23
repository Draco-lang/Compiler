using System.Collections.Generic;
using System.Collections.Immutable;
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

    private Compilation compilation;
    private SemanticModel semanticModel;
    private SyntaxTree syntaxTree;

    private readonly CompletionService completionService;
    private readonly SignatureService signatureService;
    private readonly CodeFixService codeFixService;

    public DracoLanguageServer(ILanguageClient client)
    {
        this.client = client;
        this.configurationRepository = new(client);

        // Some empty defaults
        this.syntaxTree = SyntaxTree.Create(SyntaxFactory.CompilationUnit());
        this.compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(this.syntaxTree));
        this.semanticModel = this.compilation.GetSemanticModel(this.syntaxTree);

        this.completionService = new CompletionService();
        this.completionService.AddProvider(new KeywordCompletionProvider());
        this.completionService.AddProvider(new ExpressionCompletionProvider());
        this.completionService.AddProvider(new MemberAccessCompletionProvider());

        this.signatureService = new SignatureService();

        this.codeFixService = new CodeFixService();
        this.codeFixService.AddProvider(new ImportCodeFixProvider());
    }

    public void Dispose() { }

    public async Task InitializedAsync(InitializedParams param)
    {
        await this.configurationRepository.UpdateConfigurationAsync();
    }

    public Task ShutdownAsync() => Task.CompletedTask;
}
