using System.Diagnostics;

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

    [Theory]
    [MemberData(nameof(TestData))]
    public void RunExample(string projectFile, string verifiedFile)
    {
        // Navigate to the project directory
        var projectDirectory = Path.GetDirectoryName(projectFile);
        Directory.SetCurrentDirectory(projectDirectory!);

        // Invoke 'dotnet run'
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        process.Start();

        // Wait for the process to exit
        process.WaitForExit();

        // Verify that the process exited successfully
        Assert.Equal(0, process.ExitCode);

        // Compare output to the verified file
        var output = process.StandardOutput.ReadToEnd();
        Verify(output, sourceFile: verifiedFile);
    }
}
