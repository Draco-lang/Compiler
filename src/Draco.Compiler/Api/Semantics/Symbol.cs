using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Semantics;

/// <summary>
/// Represents a symbol in the language.
/// </summary>
public interface ISymbol
{
    public string Name { get; }
}
