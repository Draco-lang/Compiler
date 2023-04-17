using System;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ICodeAction
{
    public CodeActionRegistrationOptions CodeActionRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
        CodeActionKinds = new[] { CodeActionKind.QuickFix }
    };

    public Task<CodeAction[]?> CompleteAsync(CodeActionParams param, CancellationToken cancellationToken)
    {
        return Task.FromResult(new[] {new CodeAction()
        {
            Kind = CodeActionKind.QuickFix,
            Edit = new WorkspaceEdit()
            {
                DocumentChanges = new[]
                {
                    new TextDocumentEdit()
                    {
                        Edits = new[]
                        {
                            new TextEdit()
                            {
                                
                            }
                        }
                    }
                }
            }
        });

    }
}
