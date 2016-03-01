using JustDecompile.EngineInfrastructure;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using Telerik.JustDecompiler.External;
using Telerik.JustDecompiler.External.Interfaces;
using Telerik.JustDecompiler.Common;
using Telerik.JustDecompiler.Languages;
using Telerik.JustDecompiler.Languages.VisualBasic;
using Mono.Cecil.AssemblyResolver;

namespace JustDecompile.Tools.MSBuildProjectBuilder
{
    public static class ProjectGenerationSettingsProvider
    {
        public static ProjectGenerationSettings GetProjectGenerationSettings(string assemblyFilePath, IAssemblyInfoService assemblyInfoService,
            IFrameworkResolver frameworkResolver, VisualStudioVersion visualStudioVersion, ILanguage language)
        {
            AssemblyDefinition assembly = Telerik.JustDecompiler.Decompiler.Utilities.GetAssembly(assemblyFilePath);
            AssemblyInfo assemblyInfo = assemblyInfoService.GetAssemblyInfo(assembly, frameworkResolver);
            foreach (KeyValuePair<ModuleDefinition, FrameworkVersion> pair in assemblyInfo.ModulesFrameworkVersions)
            {
                if (pair.Value == FrameworkVersion.Unknown)
                {
                    return new ProjectGenerationSettings(true, ResourceStrings.GenerateOnlySourceFilesDueToUnknownFrameworkVersion, false);
                }
                else if (pair.Value == FrameworkVersion.WindowsCE || pair.Value == FrameworkVersion.WindowsPhone ||
                    (pair.Value == FrameworkVersion.WinRT && WinRTProjectTypeDetector.GetProjectType(assembly) == WinRTProjectType.Unknown))
                {
                    return new ProjectGenerationSettings(true, ResourceStrings.GenerateOnlySourceFilesDueToNotSupportedProjectType, false);
                }
            }

            string resultErrorMessage;
            if (visualStudioVersion == VisualStudioVersion.VS2010)
            {
                if (!CanBe2010ProjectCreated(assemblyInfo, out resultErrorMessage))
                {
                    return new ProjectGenerationSettings(false, resultErrorMessage);
                }
            }
            else if (visualStudioVersion == VisualStudioVersion.VS2012)
            {
                if (!CanBe2012ProjectCreated(assembly, out resultErrorMessage))
                {
                    return new ProjectGenerationSettings(false, resultErrorMessage);
                }
            }
            else if (visualStudioVersion == VisualStudioVersion.VS2013)
            {
                if (!CanBe2013ProjectCreated(assembly, language, out resultErrorMessage))
                {
                    return new ProjectGenerationSettings(false, resultErrorMessage);
                }
            }

            return new ProjectGenerationSettings(true);
        }

        private static bool CanBe2010ProjectCreated(AssemblyInfo assemblyInfo, out string errorMessage)
        {
            Version max = new Version(0, 0);
            foreach (KeyValuePair<ModuleDefinition, FrameworkVersion> pair in assemblyInfo.ModulesFrameworkVersions)
            {
                if (pair.Value == FrameworkVersion.WinRT)
                {
                    errorMessage = ResourceStrings.CannotCreate2010ProjectDueToWinRT;
                    return false;
                }
                else if (pair.Value == FrameworkVersion.Silverlight)
                {
                    continue;
                }

                Version current = new Version(pair.Value.ToString(false));
                if (current > max)
                {
                    max = current;
                }
            }

            if (max > new Version(4, 0))
            {
                errorMessage = ResourceStrings.CannotCreate2010ProjectDueToFramework45;
                return false;
            }
            else
            {
                errorMessage = null;
                return true;
            }
        }

        private static bool CanBe2012ProjectCreated(AssemblyDefinition assembly, out string errorMessage)
        {
            if (WinRTProjectTypeDetector.IsWinRTAssemblyGeneratedWithVS2013(assembly))
            {
                errorMessage = ResourceStrings.CannotCreate2012Project;
                return false;
            }
            else if (WinRTProjectTypeDetector.IsUniversalWindowsPlatformAssembly(assembly))
            {
                errorMessage = string.Format(ResourceStrings.CannotCreateProjectDueToUWP, 2012);
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool CanBe2013ProjectCreated(AssemblyDefinition assembly, ILanguage language, out string errorMessage)
        {
            TargetPlatform targetPlatform = assembly.MainModule.AssemblyResolver.GetTargetPlatform(assembly.MainModule.FilePath);
            if (targetPlatform == TargetPlatform.WinRT && language is VisualBasic &&
                    WinRTProjectTypeDetector.GetProjectType(assembly) == WinRTProjectType.ComponentForUniversal)
            {
                errorMessage = ResourceStrings.CannotCreate2013Project;
                return false;
            }
            else if (WinRTProjectTypeDetector.IsUniversalWindowsPlatformAssembly(assembly))
            {
                errorMessage = string.Format(ResourceStrings.CannotCreateProjectDueToUWP, 2013);
                return false;
            }
            
            errorMessage = null;
            return true;
        }
    }
}
