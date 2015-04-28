using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Telerik.JustDecompiler.Decompiler.Caching;
using Telerik.JustDecompiler.Decompiler.WriterContextServices;
using Telerik.JustDecompiler.External.Interfaces;
using Telerik.JustDecompiler.Languages;
using JustDecompile.EngineInfrastructure;
using System.IO;
using Telerik.JustDecompiler.Languages.CSharp;
using Telerik.JustDecompiler.Languages.VisualBasic;
using Telerik.JustDecompiler.External;

namespace JustDecompile.Tools.MSBuildProjectBuilder
{
    public class WinRTProjectBuilder : MSBuildProjectBuilder
    {
        public const string CSharpGUID = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
        public const string VisualBasicGUID = "F184B08F-C81C-45F6-A57F-5ABD9991F28F";

        private const string WindowsStoreAppGUID = "{BC8A1FFA-BEE3-4634-8014-F334798102B3}";
        private const string PortableClassLibraryGUID = "{786C830F-07A1-408B-BD7F-6EE04809D6DB}";
        private const string WindowsPhoneAppGUID = "{76F1466A-8B6D-4E39-A767-685A06062A39}";

        private WinRTProjectType projectType;
        private ICollection<string> platforms;

        public WinRTProjectBuilder(string assemblyPath, AssemblyDefinition assembly,
            Dictionary<ModuleDefinition, Mono.Collections.Generic.Collection<TypeDefinition>> userDefinedTypes,
            Dictionary<ModuleDefinition, Mono.Collections.Generic.Collection<Resource>> resources,
            string targetPath, ILanguage language, IDecompilationPreferences preferences, IAssemblyInfoService assemblyInfoService, VisualStudioVersion visualStudioVersion, ProjectGenerationSettings projectGenerationSettings = null)
            : base(assemblyPath, assembly, userDefinedTypes, resources, targetPath, language, null, preferences, assemblyInfoService, visualStudioVersion, projectGenerationSettings)
        {
            Initialize();
        }

        public WinRTProjectBuilder(string assemblyPath, string targetPath, ILanguage language,
            IDecompilationPreferences preferences, IFileGenerationNotifier notifier,
            IAssemblyInfoService assemblyInfoService, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010,
            ProjectGenerationSettings projectGenerationSettings = null)
            : base(assemblyPath, targetPath, language, null, preferences, notifier, assemblyInfoService, visualStudioVersion, projectGenerationSettings)
        {
            Initialize();
        }

        protected override TypeCollisionWriterContextService GetWriterContextService()
        {
            return new WinRTWriterContextService(new ProjectGenerationDecompilationCacheService(), decompilationPreferences.RenameInvalidMembers);
        }

        protected override ProjectImport GenerateLanguageTargetsProjectImportProperty()
        {
            if (this.projectType == WinRTProjectType.ComponentForUniversal)
            {
                if (this.language is CSharp)
                {
                    return new ProjectImport() { Project = @"$(MSBuildExtensionsPath32)\Microsoft\Portable\$(TargetFrameworkVersion)\Microsoft.Portable.CSharp.targets" };
                }
                else if (this.language is VisualBasic)
                {
                    return new ProjectImport() { Project = @"$(MSBuildExtensionsPath32)\Microsoft\Portable\$(TargetFrameworkVersion)\Microsoft.Portable.VisualBasic.targets" };
                }
            }
            else
            {
                if (this.language is CSharp)
                {
                    return new ProjectImport() { Project = @"$(MSBuildExtensionsPath)\Microsoft\WindowsXaml\v$(VisualStudioVersion)\Microsoft.Windows.UI.Xaml.CSharp.targets" };
                }
                else if (this.language is VisualBasic)
                {
                    return new ProjectImport() { Project = @"$(MSBuildExtensionsPath)\Microsoft\WindowsXaml\v$(VisualStudioVersion)\Microsoft.Windows.UI.Xaml.VisualBasic.targets" };
                }
            }
            
            throw new NotSupportedException("Project generation not supported in this language.");
        }

        protected override string GetOutputType(ModuleDefinition module)
        {
            if (module.IsMain)
            {
                return "winmdobj";
            }
            else
            {
                // Not sure about this case, because can't generate multi-module assembly for WinRT.
                return "module";
            }
        }

        protected override string GetTargetFrameworkVersion(ModuleDefinition module)
        {
            if (this.projectType == WinRTProjectType.ComponentForUniversal)
            {
                string[] versionStringParts = this.assembly.TargetFrameworkAttributeValue.Split(',');
                string[] versionPart = versionStringParts[1].Split('=');

                return versionPart[1];
            }

            return null;
        }

        protected override ProjectPropertyGroup GetMainModuleBasicProjectProperties()
        {
            ProjectPropertyGroup result = base.GetMainModuleBasicProjectProperties();

            this.AddAdditionalProjectProperties(result);

            return result;
        }

        protected override ProjectPropertyGroup GetNetmoduleBasicProjectProperties(ModuleDefinition module)
        {
            ProjectPropertyGroup result = base.GetNetmoduleBasicProjectProperties(module);

            this.AddAdditionalProjectProperties(result);

            return result;
        }

        protected override List<string> GetConfigurationConstants(bool debugConfiguration)
        {
            List<string> result = base.GetConfigurationConstants(debugConfiguration);

            if (this.projectType != WinRTProjectType.ComponentForUniversal)
            {
                result.Add("NETFX_CORE");
            }

            if (this.language is CSharp)
            {
                if (this.projectType == WinRTProjectType.ComponentForWindows)
                {
                    result.Add("WINDOWS_APP");
                }
                else if (this.projectType == WinRTProjectType.ComponentForWindowsPhone)
                {
                    result.Add("WINDOWS_PHONE_APP");
                }
            }

            return result;
        }

        protected override IList<string> GetWarningConfigurations()
        {
            IList<string> result = base.GetWarningConfigurations();
            result.Add("42314");

            return result;
        }

        protected override object[] GetProjectItems(ModuleDefinition module, ProjectPropertyGroup basicProjectProperties)
        {
            object[] items = null;
            bool isVisualBasic = this.language is VisualBasic;
            if (this.projectType == WinRTProjectType.Component || this.projectType == WinRTProjectType.ComponentForWindows)
            {
                items = isVisualBasic ? new object[15] : new object[14];

                int i = 0;
                items[i++] = this.GenerateCommonPropsProjectImportProperty();
                items[i++] = basicProjectProperties;
                object[] configurations = this.GetConfigurations(basicProjectProperties);
                for (int j = 0; j < configurations.Length; j++, i++)
                {
                    items[j + 2] = configurations[j];
                }

                items[i++] = this.CreatePojectReferences(module, basicProjectProperties);
                items[i++] = this.fileGenContext.GetProjectItemGroup();
                items[i++] = this.GetVisualStudioVersionPropertyGroup();
                if (isVisualBasic)
                {
                    items[i++] = this.GetCompileOptions();
                }

                items[i++] = this.GenerateLanguageTargetsProjectImportProperty();
            }
            else if (this.projectType == WinRTProjectType.ComponentForUniversal)
            {
                items = new object[8];

                items[0] = this.GenerateCommonPropsProjectImportProperty();
                items[1] = basicProjectProperties;
                object[] configurations = this.GetConfigurations(basicProjectProperties);
                for (int i = 0; i < configurations.Length; i++)
                {
                    items[i + 2] = configurations[i];
                }

                items[4] = this.CreatePojectReferences(module, basicProjectProperties);
                items[5] = new ProjectItemGroup()
                {
                    TargetPlatform = new ProjectItemGroupTargetPlatform[]
                    {
                        new ProjectItemGroupTargetPlatform() { Include = "WindowsPhoneApp, Version=8.1" },
                        new ProjectItemGroupTargetPlatform() { Include = "Windows, Version=8.1" }
                    }
                };
                items[6] = this.fileGenContext.GetProjectItemGroup();
                items[7] = this.GenerateLanguageTargetsProjectImportProperty();
            }
            else if (this.projectType == WinRTProjectType.ComponentForWindowsPhone)
            {
                items = isVisualBasic ? new object[14] : new object[13];

                int i = 0;
                items[i++] = this.GenerateCommonPropsProjectImportProperty();
                items[i++] = basicProjectProperties;
                object[] configurations = this.GetConfigurations(basicProjectProperties);
                for (int j = 0; j < configurations.Length; j++, i++)
                {
                    items[j + 2] = configurations[j];
                }

                items[i++] = this.CreatePojectReferences(module, basicProjectProperties);
                items[i++] = this.fileGenContext.GetProjectItemGroup();
                items[i++] = this.GetVisualStudioVersionPropertyGroup();
                items[i++] = new ProjectPropertyGroup() { Condition = " '$(TargetPlatformIdentifier)' == '' ", TargetPlatformIdentifier = "WindowsPhoneApp" };
                if (isVisualBasic)
                {
                    items[i++] = this.GetCompileOptions();
                }

                items[i++] = this.GenerateLanguageTargetsProjectImportProperty();
            }

            return items;
        }

        protected override void WriteSolutionFile()
        {
            WinRTSolutionWriter solutionWriter =
                new WinRTSolutionWriter(this.assembly, this.platform, this.targetDir, this.filePathsService.GetSolutionRelativePath(),
                                        this.modulesToProjectsFilePathsMap, this.modulesProjectsGuids, this.visualStudioVersion,
                                        this.language, this.platforms);
            solutionWriter.WriteSolutionFile();
        }

        private void AddAdditionalProjectProperties(ProjectPropertyGroup project)
        {
            project.ProjectTypeGuids = this.GetProjectTypeGuids(this.assembly.MainModule);
            project.MinimumVisualStudioVersion = this.GetMinimumVisualStudioVersion();
            project.TargetPlatformVersion = this.GetTargetPlatformVersion();
            project.TargetFrameworkProfile = this.GetTargetFrameworkProfile();
        }

        private ProjectPropertyGroup[] GetConfigurations(ProjectPropertyGroup basicProjectProperties)
        {
            int i = 0;
            ProjectPropertyGroup[] configurations = new ProjectPropertyGroup[this.platforms.Count * 2];
            foreach (string platform in this.platforms)
            {
                if (platform == "Any CPU")
                {
                    ProjectPropertyGroup configuration;
                    bool isVisualBasic = this.language is VisualBasic;

                    configuration = base.CreateConfiguration("AnyCPU", true); //Debug
                    if (isVisualBasic)
                    {
                        configuration.NoConfig = true;
                        configuration.NoConfigSpecified = true;
                    }

                    configurations[i++] = configuration;

                    configuration = base.CreateConfiguration("AnyCPU", false); //Release
                    if (isVisualBasic)
                    {
                        configuration.NoStdLib = true;
                        configuration.NoStdLibSpecified = true;
                        configuration.NoConfig = true;
                        configuration.NoConfigSpecified = true;
                    }

                    configurations[i++] = configuration;
                }
                else
                {
                    configurations[i++] = this.CreateConfiguration(platform, true);
                    configurations[i++] = this.CreateConfiguration(platform, false);
                }
            }

            return configurations;
        }

        protected override ProjectPropertyGroup CreateConfiguration(string platform, bool debugConfiguration)
        {
            ProjectPropertyGroup config = base.CreateConfiguration(platform, debugConfiguration);

            config.UseVSHostingProcess = false;
            config.UseVSHostingProcessSpecified = true;
            config.Prefer32Bit = true;
            config.Prefer32BitSpecified = true;
            
            if (this.language is CSharp)
            {
                config.NoWarn = ";2008";
                config.WarningLevelSpecified = false;
            }

            return config;
        }

        protected override string GetOutputPath(string platform, bool debugConfiguration)
        {
            if (platform == "AnyCPU")
            {
                return base.GetOutputPath(platform, debugConfiguration);
            }

            if (debugConfiguration)
            {
                return @"bin\" + platform + @"\Debug\";
            }
            else
            {
                return @"bin\" + platform + @"\Release\";
            }
        }

        private ProjectPropertyGroup GetVisualStudioVersionPropertyGroup()
        {
            string visualStudioProductVersion;
            if (this.visualStudioVersion == VisualStudioVersion.VS2012)
            {
                visualStudioProductVersion = "11.0";
            }
            else if (this.visualStudioVersion == VisualStudioVersion.VS2013)
            {
                visualStudioProductVersion = "12.0";
            }
            else
            {
                throw new NotSupportedException();
            }

            return new ProjectPropertyGroup() { Condition = " '$(VisualStudioVersion)' == '' or '$(VisualStudioVersion)' < '" + visualStudioProductVersion + "' ", VisualStudioVersion = visualStudioProductVersion };
        }

        private string GetProjectTypeGuids(ModuleDefinition module)
        {
            string result = string.Empty;
            if (this.projectType == WinRTProjectType.Unknown)
            {
                return null;
            }

            if (this.projectType == WinRTProjectType.Component || this.projectType == WinRTProjectType.ComponentForWindows)
            {
                result += WindowsStoreAppGUID;
            }
            else if (this.projectType == WinRTProjectType.ComponentForUniversal)
            {
                result += PortableClassLibraryGUID;
            }
            else if (this.projectType == WinRTProjectType.ComponentForWindowsPhone)
            {
                result += WindowsPhoneAppGUID;
            }

            result += ";";
            if (this.language is CSharp)
            {
                result += ("{" + CSharpGUID + "}");
            }
            else if (this.language is VisualBasic)
            {
                result += ("{" + VisualBasicGUID + "}");
            }

            return result;
        }

        private string GetMinimumVisualStudioVersion()
        {
            if (this.projectType == WinRTProjectType.ComponentForUniversal ||
                this.projectType == WinRTProjectType.ComponentForWindows ||
                this.projectType == WinRTProjectType.ComponentForWindowsPhone)
            {
                return "12.0";
            }

            return null;
        }

        private string GetTargetPlatformVersion()
        {
            if (this.projectType == WinRTProjectType.ComponentForWindows || this.projectType == WinRTProjectType.ComponentForWindowsPhone)
            {
                return "8.1";
            }

            return null;
        }

        private string GetTargetFrameworkProfile()
        {
            if (this.projectType == WinRTProjectType.ComponentForUniversal)
            {
                string[] versionStringParts = this.assembly.TargetFrameworkAttributeValue.Split(',');
                string[] profilePart = versionStringParts[2].Split('=');

                return profilePart[1];
            }

            return null;
        }

        private void Initialize()
        {
            this.projectType = WinRTProjectTypeDetector.GetProjectType(this.assembly);
            if (this.projectType == WinRTProjectType.Component || this.projectType == WinRTProjectType.ComponentForWindows)
            {
                this.platforms = new List<string>() { "Any CPU", "ARM", "x64", "x86" };
            }
            else if (this.projectType == WinRTProjectType.ComponentForUniversal)
            {
                this.platforms = new List<string>() { "Any CPU" };
            }
            else if (this.projectType == WinRTProjectType.ComponentForWindowsPhone)
            {
                this.platforms = new List<string>() { "Any CPU", "ARM", "x86" };
            }
            else
            {
                this.platforms = new List<string>();
            }
        }
    }
}
