using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Locator;
using MSBuildProjectCollection = Microsoft.Build.Evaluation.ProjectCollection;

namespace Draco.ProjectSystem;

/// <summary>
/// Represents a workspace with a solution and a collection of projects.
/// </summary>
public sealed class Workspace
{
    /// <summary>
    /// Initializes a new workspace.
    /// </summary>
    /// <param name="workspacePath">The path to the workspace.</param>
    /// <returns>The created workspace.</returns>
    public static Workspace Initialize(string workspacePath)
    {
        // TODO: Add a way to customize SDK path
        // Initialize MSBuild
        MSBuildLocator.RegisterDefaults();
        return new(new(workspacePath));
    }

    /// <summary>
    /// The workspace root.
    /// </summary>
    public DirectoryInfo Root { get; }

    /// <summary>
    /// The projects in the workspace.
    /// </summary>
    public IEnumerable<Project> Projects => this.Root
        // TODO: Extract filters?
        .GetFiles("*.dracoproj", SearchOption.AllDirectories)
        .Select(this.LoadProject);

    /// <summary>
    /// The MSBuild project collection.
    /// </summary>
    internal MSBuildProjectCollection ProjectCollection { get; } = new();

    private readonly Dictionary<string, Project> loadedProjects = [];

    private Workspace(DirectoryInfo root)
    {
        this.Root = root;
    }

    private Project LoadProject(FileInfo projectFile)
    {
        if (!this.loadedProjects.TryGetValue(projectFile.FullName, out var project))
        {
            project = new Project(projectFile, this);
            this.loadedProjects.Add(projectFile.FullName, project);
        }
        return project;
    }
}
