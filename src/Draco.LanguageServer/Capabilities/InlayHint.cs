using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : IInlayHint
{
    public InlayHintRegistrationOptions InlayHintRegistrationOptions => new()
    {
        DocumentSelector = DocumentSelector,
    };

    public Task<IList<InlayHint>> InlayHintAsync(InlayHintParams param, CancellationToken cancellationToken)
    {
        // Get relevant config
        var config = this.configurationRepository.InlayHints;

        var range = Translator.ToCompiler(param.Range);

        var inlayHints = new List<InlayHint>();

        foreach (var node in this.syntaxTree.TraverseSubtreesIntersectingRange(range))
        {
            if (config.VariableTypes && node is VariableDeclarationSyntax varDecl)
            {
                // Type is already specified by user
                if (varDecl.Type is not null) continue;

                var symbol = this.semanticModel.GetDefinedSymbol(varDecl);
                if (symbol is not IVariableSymbol varSymbol) continue;

                var varType = varSymbol.Type;
                var position = varDecl.Name.Range.End;

                inlayHints.Add(new InlayHint()
                {
                    Position = Translator.ToLsp(position),
                    Kind = InlayHintKind.Type,
                    Label = $": {varType}",
                });
            }
            else if (config.ParameterNames && node is CallExpressionSyntax call)
            {
                var symbol = this.semanticModel.GetReferencedSymbol(call.Function);
                if (symbol is not IFunctionSymbol funcSymbol) continue;

                foreach (var (argSyntax, paramSymbol) in call.ArgumentList.Values.Zip(funcSymbol.Parameters))
                {
                    var position = argSyntax.Range.Start;
                    var name = paramSymbol.Name;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    inlayHints.Add(new InlayHint()
                    {
                        Position = Translator.ToLsp(position),
                        Kind = InlayHintKind.Parameter,
                        Label = $"{name} = ",
                    });
                }
            }
        }

        return Task.FromResult<IList<InlayHint>>(inlayHints);
    }
}
