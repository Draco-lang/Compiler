using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Coverage;

/// <summary>
/// The result of a coverage run.
/// </summary>
public sealed class CoverageResult(ImmutableArray<CoverageEntry> entires)
{
    /// <summary>
    /// The coverage entries.
    /// </summary>
    public ImmutableArray<CoverageEntry> Entires { get; } = entires;
}
