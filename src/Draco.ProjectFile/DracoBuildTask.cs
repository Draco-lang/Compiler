using System;
using System.IO;
using Microsoft.Build.Framework;

namespace Draco.ProjectFile
{
    public class DracoBuildTask : Microsoft.Build.Utilities.Task
    {
        public override bool Execute()
        {
            File.WriteAllText(@"C:\users\kubab\downloads\log.txt", this.ProjectDirectory);
            return true;
        }

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
    }
}
