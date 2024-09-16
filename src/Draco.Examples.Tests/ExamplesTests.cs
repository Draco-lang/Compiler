using System.Diagnostics;
using System.Text;
using DiffEngine;

namespace Draco.Examples.Tests;

public sealed class ExamplesTests
{
    public static IEnumerable<object[]> TestData
    {
        get
        {
            // Get all example projects
            // We exclude the "Toolchain" folder, in case the user has installed it in the examples directory
            var exampleProjectDirectories = Directory
                .GetDirectories("examples", "*", SearchOption.TopDirectoryOnly)
                .Where(d => Path.GetFileName(d) != "Toolchain");
            // Each directory contains a projectfile, and a verification file, return each pair
            return exampleProjectDirectories
                .Select(directory => new object[]
                {
                    // Search for the dracoproj file
                    Directory.GetFiles(directory, "*.dracoproj").Single(),
                    // The verification file is always named "verify.txt"
                    Path.Combine(directory, "verify.txt"),
                });
        }
    }

    public ExamplesTests()
    {
        DiffTools.UseOrder(DiffTool.VisualStudioCode, DiffTool.VisualStudio, DiffTool.Rider);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task RunExample(string projectFile, string verifiedFile)
    {
        // Invoke 'dotnet run' on the project
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList = { "run", "--project", projectFile },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        process.Start();

        var standardOutput = new StringBuilder();
        while (!process.StandardOutput.EndOfStream)
        {
            var line = process.StandardOutput.ReadLine();
            standardOutput.AppendLine(line);
        }
        var gotOutput = standardOutput.ToString();

        // Wait for the process to exit
        process.WaitForExit();

        // Verify that the process exited successfully
        Assert.Equal(0, process.ExitCode);

        // Configure verifier
        var settings = new VerifySettings();
        settings.UseDirectory(Path.Combine("../../", Path.GetDirectoryName(verifiedFile) ?? string.Empty));
        settings.UseFileName("expected");

        // Compare output to the verified file
        await Verify(gotOutput, settings);
    }
}
