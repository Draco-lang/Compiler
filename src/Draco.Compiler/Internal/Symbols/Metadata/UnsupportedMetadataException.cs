using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Thrown if we don't support a certain metadata construct.
/// </summary>
internal sealed class UnsupportedMetadataException : Exception
{
}
