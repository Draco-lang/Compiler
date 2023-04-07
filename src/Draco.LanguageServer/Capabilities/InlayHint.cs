using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api;
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
        // TODO: Share compilation
        var souceText = this.documentRepository.GetDocument(param.TextDocument.Uri);
        var syntaxTree = SyntaxTree.Parse(souceText);
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var inlayHints = new List<InlayHint>();

        foreach (var node in syntaxTree.TraverseSubtreesIntersectingRange(range))
        {
            if (config.VariableTypes && node is VariableDeclarationSyntax varDecl)
            {
                // Type is already specified by user
                if (varDecl.Type is not null) continue;

                var symbol = semanticModel.GetDefinedSymbol(varDecl);
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
                var symbol = semanticModel.GetReferencedSymbol(call.Function);
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
