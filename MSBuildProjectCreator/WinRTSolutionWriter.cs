using Mono.Cecil;
using Mono.Cecil.AssemblyResolver;
using System;
using System.Collections.Generic;
using System.IO;
using Telerik.JustDecompiler.Languages;
namespace JustDecompile.Tools.MSBuildProjectBuilder
{
    internal class WinRTSolutionWriter : SolutionWriter
    {
        private readonly IEnumerable<string> platforms;

        internal WinRTSolutionWriter(AssemblyDefinition assembly, TargetPlatform targetPlatform, string targetDir, string solutionFileName, 
			Dictionary<ModuleDefinition, string> modulesProjectsRelativePaths, Dictionary<ModuleDefinition, Guid> modulesProjectsGuids,
            VisualStudioVersion visualStudioVersion, ILanguage language, IEnumerable<string> platforms)
            : base(assembly, targetPlatform, targetDir, solutionFileName, modulesProjectsRelativePaths, modulesProjectsGuids, visualStudioVersion, language)
        {
            this.platforms = platforms;
        }

        protected override void WriteSolutionConfigurations(StreamWriter writer)
        {
            foreach (string platform in this.platforms)
            {
                writer.WriteLine("\t\tDebug|" + platform + " = Debug|" + platform);
            }

            foreach (string platform in this.platforms)
            {
                writer.WriteLine("\t\tRelease|" + platform + " = Release|" + platform);
            }
        }

        protected override void WriteProjectConfigurations(StreamWriter writer, ModuleDefinition module, string moduleProjectGuidString)
        {
            foreach (string platform in this.platforms)
            {
                writer.WriteLine("\t\t{" + moduleProjectGuidString + "}.Debug|" + platform + ".ActiveCfg = Debug|" + platform);
                writer.WriteLine("\t\t{" + moduleProjectGuidString + "}.Debug|" + platform + ".Build.0 = Debug|" + platform);
            }

            foreach (string platform in this.platforms)
            {
                writer.WriteLine("\t\t{" + moduleProjectGuidString + "}.Release|" + platform + ".ActiveCfg = Release|" + platform);
                writer.WriteLine("\t\t{" + moduleProjectGuidString + "}.Release|" + platform + ".Build.0 = Release|" + platform);
            }
        }
    }
}
