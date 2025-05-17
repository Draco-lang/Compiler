namespace Draco.Examples.Tests;

/// <summary>
/// Utilities for these tests.
/// </summary>
internal static class TestUtils
{
    /// <summary>
    /// A string describing that the toolchain needs to be installed in the examples directory.
    /// </summary>
    public const string DescriptionForNotInstalledToolchain = """
        Note, that you need to have the toolchain installed in the examples directory, in order to run these tests.
        You can do that by running the install_toolchain.ps1 script in the scripts directory and passing in the path to the examples directory.
        """;

    /// <summary>
    /// The root examples directory.
    /// </summary>
    public const string ExamplesDirectory = "examples";

    /// <summary>
    /// Retrieves all example project directories.
    /// </summary>
    public static IEnumerable<string> ExampleDirectories => Directory
        .GetDirectories(ExamplesDirectory, "*", SearchOption.TopDirectoryOnly)
        .Where(d => Path.GetFileName(d) != "Toolchain");
}
