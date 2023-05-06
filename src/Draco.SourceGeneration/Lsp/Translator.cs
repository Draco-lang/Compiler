using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cs = Draco.SourceGeneration.Lsp.CsModel;
using Ts = Draco.SourceGeneration.Lsp.Metamodel;

namespace Draco.SourceGeneration.Lsp;

/// <summary>
/// Translates the TS-based metamodel into a C# model.
/// </summary>
internal sealed class Translator
{
    private readonly Ts.MetaModel sourceModel;
    private readonly Cs.Model targetModel = new();

    public Translator(Ts.MetaModel sourceModel)
    {
        this.sourceModel = sourceModel;
    }

    public Cs.Model Translate()
    {
        // Translate
        foreach (var structure in this.sourceModel.Structures) this.TranslateStructure(structure);
        foreach (var enumeration in this.sourceModel.Enumerations) this.TranslateEnumeration(enumeration);
        foreach (var typeAlias in this.sourceModel.TypeAliases) this.TranslateTypeAlias(typeAlias);

        // Connect up hierarchy
        foreach (var @class in this.targetModel.Declarations.OfType<Cs.Class>()) @class.InitializeParents();

        return this.targetModel;
    }

    private void TranslateStructure(Ts.Structure structure)
    {
        var result = TranslateDeclaration<Cs.Class>(structure);
        this.targetModel.Declarations.Add(result);

        // TODO
    }

    private void TranslateStructureAsInterface(Ts.Structure structure)
    {
        var result = TranslateDeclaration<Cs.Interface>(structure);
        this.targetModel.Declarations.Add(result);

        // TODO
    }

    private void TranslateEnumeration(Ts.Enumeration enumeration)
    {
        var result = TranslateDeclaration<Cs.Enum>(enumeration);
        this.targetModel.Declarations.Add(result);

        // TODO
    }

    private void TranslateTypeAlias(Ts.TypeAlias typeAlias)
    {
        // TODO
    }

    private static TDeclaration TranslateDeclaration<TDeclaration>(Ts.IDocumented source)
        where TDeclaration : Cs.Declaration, new()
    {
        var target = new TDeclaration();

        if (source is Ts.IDeclaration declSource) target.Name = declSource.Name;

        target.Documentation = source.Documentation;
        target.Deprecated = source.Deprecated;
        target.SinceVersion = source.Since;
        target.IsProposed = source.Proposed ?? false;

        return target;
    }
}
