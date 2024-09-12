using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeCompletion.Providers;

/// <summary>
/// Provides completion for expressions excluding member access.
/// </summary>
public sealed class ExpressionCompletionProvider : CompletionProvider
{
    public override bool IsApplicableIn(CompletionContext context)
    {
        if (context.HasFlag(CompletionContext.Member)) return false;
        return context.HasFlag(CompletionContext.Expression)
            || context.HasFlag(CompletionContext.Type)
            || context.HasFlag(CompletionContext.Import);
    }

    public override ImmutableArray<CompletionItem> GetCompletionItems(
        SemanticModel semanticModel, int cursorIndex, CompletionContext contexts)
    {
        var tree = semanticModel.Tree;
        var cursor = tree.IndexToSyntaxPosition(cursorIndex);
        var syntax = tree.Root.TraverseSubtreesAtCursorPosition(cursor).LastOrDefault();
        if (syntax is null) return [];
        var symbols = semanticModel.GetAllAccessibleSymbols(syntax);
        var span = (syntax as SyntaxToken)?.Span ?? new(cursorIndex, 0);
        var completions = symbols
            // NOTE: Grouping by GetType is very error-prone, maybe we need a symbol "kind"
            .GroupBy(x => (x.GetType(), x.Name))
            .Select(x => GetCompletionItem(tree.SourceText, [.. x], contexts, span));
        return completions.OfType<CompletionItem>().ToImmutableArray();
    }

    private static CompletionItem? GetCompletionItem(
        SourceText source, ImmutableArray<ISymbol> symbols, CompletionContext currentContexts, SourceSpan span)
    {
        var sym = symbols.First();
        // NOTE: We should have something for this in the API
        while (sym is IAliasSymbol alias) sym = alias.Substitution;
        return sym switch
        {
            ITypeSymbol t when currentContexts.HasFlag(CompletionContext.Expression)
                            || currentContexts.HasFlag(CompletionContext.Type) =>
                CompletionItem.Create(
                    source,
                    t.Name,
                    span,
                    symbols,
                    t.IsValueType ? CompletionKind.ValueTypeName : CompletionKind.ReferenceTypeName),

            IParameterSymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.ParameterName),

            IVariableSymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.VariableName),

            IPropertySymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.PropertyName),

            IFieldSymbol when currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.FieldName),

            // We need the type context here for qualified type references
            IModuleSymbol when currentContexts.HasFlag(CompletionContext.Expression)
                            || currentContexts.HasFlag(CompletionContext.Type)
                            || currentContexts.HasFlag(CompletionContext.Import) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.ModuleName),

            IFunctionSymbol fun when !fun.IsSpecialName && currentContexts.HasFlag(CompletionContext.Expression) =>
                CompletionItem.Create(source, symbols.First().Name, span, symbols, CompletionKind.FunctionName),

            _ => null,
        };
    }
}
