using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.CodeCompletion;
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

    public Task<SignatureHelp?> FormatTextDocumentAsync(SignatureHelpParams param, CancellationToken cancellationToken)
    {
        var cursorPosition = Translator.ToCompiler(param.Position);
        var service = new SignatureService();
        return Task.FromResult(Translator.ToLsp(service.GetSignature(this.syntaxTree, this.semanticModel, cursorPosition)));
    }
}
