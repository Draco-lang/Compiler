using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis.Domain;

/// <summary>
/// A domain that is always full.
/// </summary>
internal sealed class FullDomain : ValueDomain
{
    /// <summary>
    /// A singleton instance to use.
    /// </summary>
    public static FullDomain Instance { get; } = new();

    public override bool IsEmpty => false;

    private FullDomain()
    {
    }

    public override ValueDomain Clone() => Instance;
    public override void SubtractPattern(BoundPattern pattern) { }
    public override BoundPattern? SamplePattern() => null;
    public override string ToString() => "<full>";
}
