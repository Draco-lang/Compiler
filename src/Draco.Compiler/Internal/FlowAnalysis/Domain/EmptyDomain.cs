using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis.Domain;

/// <summary>
/// Represents a domain that's empty.
/// </summary>
internal sealed class EmptyDomain : ValueDomain
{
    /// <summary>
    /// A singleton instance to use.
    /// </summary>
    public static EmptyDomain Instance { get; } = new();

    public override bool IsEmpty => true;

    private EmptyDomain()
    {
    }

    public override ValueDomain Clone() => Instance;
    public override void SubtractPattern(BoundPattern pattern) { }
    public override BoundPattern? SamplePattern() => null;
    public override string ToString() => "<empty>";
}
