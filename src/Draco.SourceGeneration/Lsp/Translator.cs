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
        var result = new Cs.Class
        {
            Name = structure.Name,
            Documentation = structure.Documentation,
            Deprecated = structure.Deprecated,
        };
        this.targetModel.Declarations.Add(result);

        // TODO
    }

    private void TranslateEnumeration(Ts.Enumeration enumeration)
    {
        var result = new Cs.Enum
        {
            Name = enumeration.Name,
            Documentation = enumeration.Documentation,
            Deprecated = enumeration.Deprecated,
        };
        // TODO
    }

    private void TranslateTypeAlias(Ts.TypeAlias typeAlias)
    {
        // TODO
    }
}
