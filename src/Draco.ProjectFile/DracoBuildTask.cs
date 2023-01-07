using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;

namespace Draco.ProjectFile
{
    public class DracoBuildTask : Microsoft.Build.Utilities.ToolTask
    {
        // TODO: Emit outputFileName.runtimeconfig.json, for start we just need
        //{
        //  "runtimeOptions": {
        //    "tfm": "net7.0",
        //    "framework": {
        //      "name": "Microsoft.NETCore.App",
        //      "version": "7.0.0"
        //      }
        //  }
        //}
        public override bool Execute()
        {
            var files = Directory.EnumerateFiles(this.ProjectDirectory, "*.draco", SearchOption.TopDirectoryOnly).Where(x => x == Path.Combine(this.ProjectDirectory, "main.draco"));
            if (files.Count() == 0) return false;
            var mainFile = files.First();
            var output = $"{Path.GetFileNameWithoutExtension(mainFile)}.exe";
            this.Log.LogMessage(this.GenerateFullPathToTool());
            this.ExecuteTool(this.GenerateFullPathToTool(), "", $"compile {mainFile} {output.ToCliFlag("output")}");
            // TODO: Retarget standard output and show diags as errors/warnings/...
            return true;
        }

        protected override string GenerateFullPathToTool() => Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\..\Draco.Compiler.Cli\bin\Debug\net7.0\Draco.Compiler.Cli.exe"));

        /// <summary>
        /// Output type of the given project.
        /// </summary>
        //[Required]
        public string OutputType { get; set; }

        /// <summary>
        /// The directory the current project is located in.
        /// </summary>
        //[Required]
        public string ProjectDirectory { get; set; }

        protected override string ToolName => "Draco.Compiler.Cli.exe";
    }
    internal static class CliFlag
    {
        public static string ToCliFlag(this object flagValue, string flagName) => $"--{flagName} {flagValue}";
    }
}
