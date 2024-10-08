using System.Collections.Immutable;
using System.IO;
using System.Linq;
using MSBuildProjectInstance = Microsoft.Build.Execution.ProjectInstance;

namespace Draco.ProjectSystem;

/// <summary>
/// The result of a design-time build.
/// </summary>
public readonly struct DesignTimeBuild
{
    /// <summary>
    /// The project that was built.
    /// </summary>string
    public Project Project { get; }

    /// <summary>
    /// The reference paths of the project.
    /// </summary>
    public ImmutableArray<FileInfo> References => this.ProjectInstance
        .GetItems("Reference")
        .Select(i => new FileInfo(i.EvaluatedInclude))
        .ToImmutableArray();

    /// <summary>
    /// THe project instance that was used for the build.
    /// </summary>
    internal MSBuildProjectInstance ProjectInstance { get; }

    internal DesignTimeBuild(
        Project project,
        MSBuildProjectInstance projectInstance)
    {
        this.Project = project;
        this.ProjectInstance = projectInstance;
    }
}
