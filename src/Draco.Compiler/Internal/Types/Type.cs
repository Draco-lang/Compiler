using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// The base for all types within the language.
/// </summary>
internal abstract partial class Type
{
    public abstract override string ToString();
}
