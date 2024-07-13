using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
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
    /// THe workspace the project belongs to.
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
    internal MSBuildProject TfmProject => this.tfmProject ??= new(
        projectFile: this.ProjectFile.FullName,
        projectCollection: this.Workspace.ProjectCollection,
        globalProperties: GetGlobalProperties(),
        toolsVersion: null,
        loadSettings: ProjectLoadSettings.IgnoreMissingImports | ProjectLoadSettings.IgnoreInvalidImports);
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
    internal MSBuildProject BuildProject
    {
        get
        {
            if (this.buildProject is null)
            {
                var globalProps = GetGlobalProperties();
                var targetFramework = this.TargetFramework;
                if (targetFramework is not null)
                {
                    globalProps.Add("TargetFramework", targetFramework);
                }
                this.buildProject = new(
                    projectFile: this.ProjectFile.FullName,
                    projectCollection: this.Workspace.ProjectCollection,
                    globalProperties: globalProps,
                    toolsVersion: null,
                    loadSettings: ProjectLoadSettings.IgnoreMissingImports | ProjectLoadSettings.IgnoreInvalidImports);
            }
            return this.buildProject;
        }
    }
    private MSBuildProject? buildProject;

    internal Project(FileInfo projectFile, Workspace workspace)
    {
        this.ProjectFile = projectFile;
        this.Workspace = workspace;
    }

    /// <summary>
    /// Performs a design-time build of the project.
    /// </summary>
    /// <returns>The result of the build.</returns>
    public DesignTimeBuild BuildDesignTime()
    {
        var stringLog = new StringWriter();
        var stringLogger = new StringWriterLogger(stringLog);

        var projectInstance = this.BuildProject.CreateProjectInstance();
        var buildTargets = GetDesignTimeBuildTargets();
        var success = projectInstance.Build(buildTargets, [stringLogger]);

        return new(this, projectInstance, success, stringLog.ToString());
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
