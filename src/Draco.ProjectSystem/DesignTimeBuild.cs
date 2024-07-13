using System.Collections.Immutable;
using System.Linq;
using MSBuildProject = Microsoft.Build.Evaluation.Project;
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
    /// True, if the build succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// The build log.
    /// </summary>
    public string BuildLog { get; }

    /// <summary>
    /// The reference paths of the project.
    /// </summary>
    public ImmutableArray<string> References => this.ProjectInstance
        .GetItems("Reference")
        .Select(i => i.EvaluatedInclude)
        .ToImmutableArray();

    /// <summary>
    /// THe project instance that was used for the build.
    /// </summary>
    internal MSBuildProjectInstance ProjectInstance { get; }

    internal DesignTimeBuild(
        Project project,
        MSBuildProjectInstance projectInstance,
        bool success,
        string buildLog)
    {
        this.Project = project;
        this.ProjectInstance = projectInstance;
        this.Succeeded = success;
        this.BuildLog = buildLog;
    }
}
