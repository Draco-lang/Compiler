using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.CodeFixes;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ICodeAction
{
    public CodeActionRegistrationOptions CodeActionRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
        CodeActionKinds = new[] { CodeActionKind.QuickFix },
        ResolveProvider = false
    };

    public Task<CodeAction[]?> CompleteAsync(CodeActionParams param, CancellationToken cancellationToken)
    {
        var service = new CodeFixService();
        service.AddProvider(new ImportCodeFixProvider(this.syntaxTree, Translator.ToCompiler(param.Range)));
        var fixes = service.GetCodeFixes(this.syntaxTree, this.semanticModel);
        var actions = new CodeAction[fixes.Length];

        for (int i = 0; i < fixes.Length; i++)
        {
            actions[i] = new CodeAction()
            {
                Title = fixes[i].DisplayText,
                Kind = CodeActionKind.QuickFix, //TODO: we might have some other fixes in future
                Edit = new WorkspaceEdit()
                {
                    Changes = new Dictionary<DocumentUri, IList<Lsp.Model.TextEdit>>()
                    {
                        { param.TextDocument.Uri, fixes[i].Edits.Select(x => Translator.ToLsp(x)).ToList() }
                    }
                }
            };
        }
        return Task.FromResult<CodeAction[]?>(actions);
    }
}
