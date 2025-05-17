using System.Diagnostics;
using System.Text;
using DiffEngine;
using Draco.ProjectSystem;

namespace Draco.Examples.Tests;

// NOTE: Unnfortunately the tooling seemms to have a race condition if we attempt a design-time build here
// So we order to first run the examples, and then the design-time build
[TestCaseOrderer("Draco.Examples.Tests.PriorityOrderer", "Draco.Examples.Tests")]
public sealed class ExamplesTests
{
    // Each directory contains a projectfile, and a verification file, return each pair
    public static IEnumerable<object[]> TestData => TestUtils.ExampleDirectories
        .Select(directory => new object[]
        {
            // Search for the dracoproj file
            Directory.GetFiles(directory, "*.dracoproj").Single(),
            // The verification file is always named "verify.txt"
            Path.Combine(directory, "verify.txt"),
        });

    public ExamplesTests()
    {
        DiffTools.UseOrder(DiffTool.VisualStudioCode, DiffTool.VisualStudio, DiffTool.Rider);
    }

    [Theory, TestPriority(1)]
    [MemberData(nameof(TestData))]
    public async Task RunExample(string projectFile, string verifiedFile)
    {
        // Invoke 'dotnet run' on the project
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "run", "--project", projectFile },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        // Skip first-time message
        startInfo.EnvironmentVariables["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        var process = new Process
        {
            StartInfo = startInfo,
        };
        process.Start();

        var standardOutput = new StringBuilder();
        while (!process.StandardOutput.EndOfStream)
        {
            var line = process.StandardOutput.ReadLine();
            standardOutput.AppendLine(line);
        }
        var gotOutput = standardOutput.ToString();

        var standardError = new StringBuilder();
        while (!process.StandardError.EndOfStream)
        {
            var line = process.StandardError.ReadLine();
            standardError.AppendLine(line);
        }
        var gotError = standardError.ToString();

        // Wait for the process to exit
        process.WaitForExit();

        // Verify that the process exited successfully
        Assert.True(process.ExitCode == 0, $"""
            The process exited with a non-zero exit code ({process.ExitCode}).
            Message: {gotError}
            {TestUtils.DescriptionForNotInstalledToolchain}
            """);

        // Configure verifier
        var settings = new VerifySettings();
        settings.UseDirectory(Path.Combine("../../", Path.GetDirectoryName(verifiedFile) ?? string.Empty));
        settings.UseFileName("expected");

        // Compare output to the verified file
        await Verify(gotOutput, settings);
    }

    [Fact, TestPriority(2)]
    public void DesignTimeBuild()
    {
        // Iniitialize the workspace
        var workspace = Workspace.Initialize(TestUtils.ExamplesDirectory);

        // Assert we have projects in there
        var projects = workspace.Projects.ToList();
        Assert.NotEmpty(projects);

        // Run the design time build for each
        foreach (var project in projects)
        {
            var buildResult = project.BuildDesignTime();
            Assert.True(buildResult.Success, buildResult.Log);
        }
    }
}
