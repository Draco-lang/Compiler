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

    public Task<SignatureHelp?> SignatureHelpAsync(SignatureHelpParams param, CancellationToken cancellationToken)
    {
        var cursorPosition = Translator.ToCompiler(param.Position);
        var signatureItems = this.signatureService.GetSignature(this.syntaxTree, this.semanticModel, cursorPosition);
        return Task.FromResult(Translator.ToLsp(signatureItems));
    }
}
