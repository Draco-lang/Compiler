using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Coverage;

/// <summary>
/// Settings for the instrumentation weaver.
/// </summary>
internal sealed class InstrumentationWeaverSettings
{
    /// <summary>
    /// The default settings.
    /// </summary>
    public static InstrumentationWeaverSettings Default { get; } = new();

    /// <summary>
    /// True, if the weaver should check for the ExcludeCoverage attribute.
    /// </summary>
    public bool CheckForExcludeCoverageAttribute { get; init; } = true;

    /// <summary>
    /// True, if the weaver should check for the CompilerGenerated attribute.
    /// </summary>
    public bool CheckForCompilerGeneratedAttribute { get; init; } = true;
}
