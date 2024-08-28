using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using MSBuildProject = Microsoft.Build.Evaluation.Project;
using MSBuildProjectInstance = Microsoft.Build.Execution.ProjectInstance;

namespace Draco.ProjectSystem;

/// <summary>
/// Represents a single project in a workspace.
/// </summary>
public sealed class Project
{
    /// <summary>
    /// The project file.
    /// </summary>
    public FileInfo ProjectFile { get; }

    /// <summary>
    /// The workspace the project belongs to.
    /// </summary>
    public Workspace Workspace { get; }

    /// <summary>
    /// The target framework of the project.
    /// </summary>
    public string? TargetFramework
    {
        get
        {
            var tfm = this.TfmProjectInstance.GetPropertyValue("TargetFramework");
            if (!string.IsNullOrEmpty(tfm)) return tfm;

            var targetFrameworks = this.TfmProjectInstance.GetPropertyValue("TargetFrameworks");
            return targetFrameworks
                .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Project for reading out the target framework(s).
    /// </summary>
    internal MSBuildProject TfmProject => this.tfmProject ??= this.CreateTfmProject();
    private MSBuildProject? tfmProject;

    /// <summary>
    /// Project instance for the target framework project.
    /// </summary>
    internal MSBuildProjectInstance TfmProjectInstance =>
        this.tfmProjectInstance ??= this.TfmProject.CreateProjectInstance();
    private MSBuildProjectInstance? tfmProjectInstance;

    /// <summary>
    /// Project for design-time builds.
    /// </summary>
    internal MSBuildProject BuildProject => this.buildProject ??= this.CreateBuildProject();
    private MSBuildProject? buildProject;

    internal Project(FileInfo projectFile, Workspace workspace)
    {
        this.ProjectFile = projectFile;
        this.Workspace = workspace;
    }

    /// <summary>
    /// Performs a restore of the project.
    /// </summary>
    /// <returns>The result of the restore.</returns>
    public BuildResult<Unit> Restore()
    {
        var log = new StringWriter();
        var logger = new StringWriterLogger(log);

        var buildTargets = GetRestoreBuildTargets();
        var projectInstance = this.BuildProject.CreateProjectInstance();
        var success = projectInstance.Build(buildTargets, [logger]);

        return success
            ? BuildResult.Success(Unit.Default, log.ToString())
            : BuildResult.Failure<Unit>(log.ToString());
    }

    /// <summary>
    /// Performs a design-time build of the project.
    /// </summary>
    /// <returns>The result of the build.</returns>
    public BuildResult<DesignTimeBuild> BuildDesignTime()
    {
        // First restore the project
        var restoreResult = this.Restore();
        if (!restoreResult.Success) return BuildResult.Failure<DesignTimeBuild>(restoreResult.Log);

        // NOTE: I have no idea why, but this is needed
        // I suppose it's because a restore introduces files since the last run, something that the API doesn't handle?
        // Mark the project as dirty
        this.BuildProject.MarkDirty();

        var log = new StringWriter();
        var logger = new StringWriterLogger(log);

        var buildTargets = GetDesignTimeBuildTargets();
        var projectInstance = this.BuildProject.CreateProjectInstance();
        var success = projectInstance.Build(buildTargets, [logger]);

        return success
            ? BuildResult.Success(new DesignTimeBuild(this, projectInstance), log.ToString())
            : BuildResult.Failure<DesignTimeBuild>(log.ToString());
    }

    private MSBuildProject CreateTfmProject() => new(
        projectFile: this.ProjectFile.FullName,
        projectCollection: this.Workspace.ProjectCollection,
        globalProperties: GetGlobalProperties(),
        toolsVersion: null,
        loadSettings: ProjectLoadSettings.IgnoreMissingImports | ProjectLoadSettings.IgnoreInvalidImports);

    private MSBuildProject CreateBuildProject()
    {
        var globalProps = GetGlobalProperties();
        var targetFramework = this.TargetFramework;
        if (targetFramework is not null)
        {
            globalProps.Add("TargetFramework", targetFramework);
        }
        return new MSBuildProject(
            projectFile: this.ProjectFile.FullName,
            projectCollection: this.Workspace.ProjectCollection,
            globalProperties: globalProps,
            toolsVersion: null,
            loadSettings: ProjectLoadSettings.IgnoreMissingImports | ProjectLoadSettings.IgnoreInvalidImports);
    }

    private static IDictionary<string, string> GetGlobalProperties() => new Dictionary<string, string>
    {
        ["ProvideCommandLineArgs"] = "true",
        ["DesignTimeBuild"] = "true",
        ["SkipCompilerExecution"] = "true",
        ["GeneratePackageOnBuild"] = "false",
        ["Configuration"] = "Debug",
        ["DefineExplicitDefaults"] = "true",
        ["BuildProjectReferences"] = "false",
        ["UseCommonOutputDirectory"] = "false",
        // Required by the Clean Target
        ["NonExistentFile"] = Path.Combine("__NonExistentSubDir__", "__NonExistentFile__"),
        ["DotnetProjInfo"] = "true",
    };

    private static string[] GetRestoreBuildTargets() => [
        "Restore"];

    private static string[] GetDesignTimeBuildTargets() => [
        "ResolveAssemblyReferencesDesignTime",
        "ResolveProjectReferencesDesignTime",
        "ResolvePackageDependenciesDesignTime",
        "FindReferenceAssembliesForReferences",
        "_GenerateCompileDependencyCache",
        "_ComputeNonExistentFileProperty",
        "BeforeBuild",
        "BeforeCompile",
        "CoreCompile"];
}
