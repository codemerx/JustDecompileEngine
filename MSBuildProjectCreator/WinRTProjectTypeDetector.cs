using System;
using System.IO;
using System.Linq;

using Mono.Cecil;

namespace JustDecompile.Tools.MSBuildProjectBuilder
{
    internal class WinRTProjectTypeDetector
    {
        public static WinRTProjectType GetProjectType(AssemblyDefinition assembly)
        {
            string fileExtension = Path.GetExtension(assembly.MainModule.FilePath);
            switch (fileExtension)
            {
                case ".winmd":
                    return DetectComponentType(assembly);
                default:
                    return WinRTProjectType.Unknown;
            }
        }

        public static bool IsWinRTAssemblyGeneratedWithVS2013(AssemblyDefinition assembly)
        {
            return assembly.MainModule.AssemblyReferences.Any(r => r.Name == "System.Runtime" && r.Version == new Version(4, 0, 10, 0));
        }

        private static WinRTProjectType DetectComponentType(AssemblyDefinition assembly)
        {
            if (!IsWinRTAssemblyGeneratedWithVS2013(assembly))
            {
                return WinRTProjectType.Component;
            }
            else if (assembly.TargetFrameworkAttributeValue.StartsWith(".NETPortable"))
            {
                return WinRTProjectType.ComponentForUniversal;
            }
            else if (assembly.TargetFrameworkAttributeValue.StartsWith(".NETCore"))
            {
                return WinRTProjectType.ComponentForWindows;
            }
            else if (assembly.TargetFrameworkAttributeValue.StartsWith("WindowsPhoneApp"))
            {
                return WinRTProjectType.ComponentForWindowsPhone;
            }
            else
            {
                return WinRTProjectType.Unknown;
            }
        }
    }
}
