using System;
using System.Collections.Generic;
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

    private Translator(Ts.MetaModel sourceModel)
    {
        this.sourceModel = sourceModel;
    }

    private void Translate()
    {
    }
}
