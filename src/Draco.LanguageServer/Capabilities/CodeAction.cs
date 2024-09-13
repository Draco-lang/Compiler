using System.Collections.Generic;
using System.Linq;
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
        CodeActionKinds = [CodeActionKind.QuickFix],
        ResolveProvider = false
    };

    public Task<IList<OneOf<Command, CodeAction>>?> CodeActionAsync(CodeActionParams param, CancellationToken cancellationToken)
    {
        var compilation = this.compilation;

        var syntaxTree = GetSyntaxTree(compilation, param.TextDocument.Uri);
        if (syntaxTree is null) return Task.FromResult(null as IList<OneOf<Command, CodeAction>>);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var range = Translator.ToCompiler(param.Range);
        var span = syntaxTree.SyntaxRangeToSourceSpan(range);
        var fixes = this.codeFixService.GetCodeFixes(syntaxTree, semanticModel, span);
        var actions = new List<OneOf<Command, CodeAction>>();

        foreach (var fix in fixes)
        {
            var translatedEdits = fix.Edits
                .Select(e => Translator.ToLsp(syntaxTree.SourceText, e))
                .ToList();

            actions.Add(new CodeAction()
            {
                Title = fix.DisplayText,
                //TODO: we might have some other fixes in future
                Kind = CodeActionKind.QuickFix,
                Edit = new WorkspaceEdit()
                {
                    Changes = new Dictionary<DocumentUri, IList<ITextEdit>>()
                    {
                        { param.TextDocument.Uri, translatedEdits }
                    }
                }
            });
        }
        return Task.FromResult<IList<OneOf<Command, CodeAction>>?>(actions);
    }
}
