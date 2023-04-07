using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A module imported from metadata.
/// </summary>
internal sealed class MetadataModuleSymbol : ModuleSymbol
{
    public override Symbol? ContainingSymbol => throw new NotImplementedException();

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();
}
