using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ISignatureHelp
{
    public SignatureHelpRegistrationOptions SignatureHelpRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
        TriggerCharacters = new[] { "(" },
        RetriggerCharacters = new[] { "," },
    };

    public Task<SignatureHelp?> SignatureHelpAsync(SignatureHelpParams param, CancellationToken cancellationToken)
    {
        var compilation = this.compilation;

        var syntaxTree = GetSyntaxTree(compilation, param.TextDocument.Uri);
        if (syntaxTree is null) return Task.FromResult(null as SignatureHelp);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var cursorPosition = Translator.ToCompiler(param.Position);
        var signatureItems = this.signatureService.GetSignature(syntaxTree, semanticModel, cursorPosition);
        return Task.FromResult(Translator.ToLsp(signatureItems));
    }
}
