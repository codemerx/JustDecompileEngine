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
using Mono.Cecil.Extensions;
using Mono.Cecil.AssemblyResolver;
using System.Xml;

namespace JustDecompile.Tools.MSBuildProjectBuilder
{
    // Consider changing the way of how the different projects are build when thos class needs to be extended further. At the moment, it is
    // done by only one class WinRTProjectBuilder, which chooses what to put in the resulting project file based on the type of the WinRT
    // assembly. Better and more maintainable solution would be class hierarchy, containing class for every project type, where that's needed. 
    public class WinRTProjectBuilder : MSBuildProjectBuilder
    {
        public const string CSharpGUID = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
        public const string VisualBasicGUID = "F184B08F-C81C-45F6-A57F-5ABD9991F28F";

        private const string WindowsStoreAppGUID = "{BC8A1FFA-BEE3-4634-8014-F334798102B3}";
        private const string PortableClassLibraryGUID = "{786C830F-07A1-408B-BD7F-6EE04809D6DB}";
        private const string WindowsPhoneAppGUID = "{76F1466A-8B6D-4E39-A767-685A06062A39}";

        private const string UWPProjectGUID = "{A5A43C5B-DE2A-4C0C-9213-0A381AF9435A}";

        private const string UAPPlatformIdentifier = "UAP";
        private readonly Version DefaultUAPVersion = new Version(10, 0, 10240, 0);

        #region NetCoreFrameworkAssemblies
        private readonly HashSet<string> NetCoreFrameworkAssemblies = new HashSet<string>()
        {
            "Microsoft.CSharp",
            "Microsoft.VisualBasic",
            "Microsoft.Win32.Primitives",
            "System.AppContext",
            "System.Collections.Concurrent",
            "System.Collections",
            "System.Collections.Immutable",
            "System.Collections.NonGeneric",
            "System.Collections.Specialized",
            "System.ComponentModel.Annotations",
            "System.ComponentModel",
            "System.ComponentModel.EventBasedAsync",
            "System.Data.Common",
            "System.Diagnostics.Contracts",
            "System.Diagnostics.Debug",
            "System.Diagnostics.StackTrace",
            "System.Diagnostics.Tools",
            "System.Diagnostics.Tracing",
            "System.Dynamic.Runtime",
            "System.Globalization.Calendars",
            "System.Globalization",
            "System.Globalization.Extensions",
            "System.IO.Compression",
            "System.IO.Compression.ZipFile",
            "System.IO",
            "System.IO.FileSystem",
            "System.IO.FileSystem.Primitives",
            "System.IO.IsolatedStorage",
            "System.IO.UnmanagedMemoryStream",
            "System.Linq",
            "System.Linq.Expressions",
            "System.Linq.Parallel",
            "System.Linq.Queryable",
            "System.Net.Http",
            "System.Net.Http.Rtc",
            "System.Net.NetworkInformation",
            "System.Net.Primitives",
            "System.Net.Requests",
            "System.Net.Sockets",
            "System.Net.WebHeaderCollection",
            "System.Numerics.Vectors",
            "System.Numerics.Vectors.WindowsRuntime",
            "System.ObjectModel",
            "System.Private.DataContractSerialization",
            "System.Private.Networking",
            "System.Private.ServiceModel",
            "System.Private.Uri",
            "System.Reflection.Context",
            "System.Reflection.DispatchProxy",
            "System.Reflection",
            "System.Reflection.Extensions",
            "System.Reflection.Metadata",
            "System.Reflection.Primitives",
            "System.Reflection.TypeExtensions",
            "System.Resources.ResourceManager",
            "System.Runtime",
            "System.Runtime.Extensions",
            "System.Runtime.Handles",
            "System.Runtime.InteropServices",
            "System.Runtime.InteropServices.WindowsRuntime",
            "System.Runtime.Numerics",
            "System.Runtime.Serialization.Json",
            "System.Runtime.Serialization.Primitives",
            "System.Runtime.Serialization.Xml",
            "System.Runtime.WindowsRuntime",
            "System.Runtime.WindowsRuntime.UI.Xaml",
            "System.Security.Claims",
            "System.Security.Principal",
            "System.ServiceModel.Duplex",
            "System.ServiceModel.Http",
            "System.ServiceModel.NetTcp",
            "System.ServiceModel.Primitives",
            "System.ServiceModel.Security",
            "System.Text.Encoding.CodePages",
            "System.Text.Encoding",
            "System.Text.Encoding.Extensions",
            "System.Text.RegularExpressions",
            "System.Threading",
            "System.Threading.Overlapped",
            "System.Threading.Tasks.Dataflow",
            "System.Threading.Tasks",
            "System.Threading.Tasks.Parallel",
            "System.Threading.Timer",
            "System.Xml.ReaderWriter",
            "System.Xml.XDocument",
            "System.Xml.XmlDocument",
            "System.Xml.XmlSerializer"
        };
        #endregion

        #region DefaultUAPReferences
        // The default UAP references for 10.0.10240.0
        private readonly HashSet<string> DefaultUAPReferences = new HashSet<string>()
        {
            "Windows.Foundation.FoundationContract",
            "Windows.Foundation.UniversalApiContract",
            "Windows.Networking.Connectivity.WwanContract"
        };
        #endregion

        private WinRTProjectType projectType;
        private Dictionary<string, string> dependencies;
        private List<string> runtimes;
        private Version minInstalledUAPVersion;
        private Version maxInstalledUAPVersion;
        private bool? isUWPProject;
        private HashSet<string> uwpReferenceAssemblies;

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

        private bool IsUWPProject
        {
            get
            {
                if (!this.isUWPProject.HasValue)
                {
                    this.isUWPProject = this.projectType == WinRTProjectType.UWPComponent ||
                                        this.projectType == WinRTProjectType.UWPLibrary ||
                                        this.projectType == WinRTProjectType.UWPApplication;
                }

                return this.isUWPProject.Value;
            }
        }

        private HashSet<string> UWPReferenceAssemblies
        {
            get
            {
                if (this.uwpReferenceAssemblies == null)
                {
                    string maxInstalledUAPDirectory = Path.Combine(SystemInformation.UAP_PLATFORM, this.maxInstalledUAPVersion.ToString());
                    string maxInstalledUAPPlatformXmlFilePath = Path.Combine(maxInstalledUAPDirectory, "Platform.xml");
                    if (Directory.Exists(maxInstalledUAPDirectory) && File.Exists(maxInstalledUAPPlatformXmlFilePath))
                    {
                        this.uwpReferenceAssemblies = new HashSet<string>();
                        XmlDocument doc = new XmlDocument();
                        doc.Load(maxInstalledUAPPlatformXmlFilePath);
                        foreach (XmlNode node in doc.SelectNodes("//ApiContract"))
                        {
                            this.uwpReferenceAssemblies.Add(node.Attributes["name"].Value);
                        }
                    }
                    else
                    {
                        this.uwpReferenceAssemblies = DefaultUAPReferences;
                    }
                }

                return this.uwpReferenceAssemblies;
            }
        }

        protected override TypeCollisionWriterContextService GetWriterContextService()
        {
            return new WinRTWriterContextService(new ProjectGenerationDecompilationCacheService(), decompilationPreferences.RenameInvalidMembers);
        }

        protected override ProjectImport GenerateLanguageTargetsProjectImportProperty()
        {
            if (this.projectType == WinRTProjectType.ComponentForUniversal)
            {
                if (this.language is ICSharp)
                {
                    return new ProjectImport() { Project = @"$(MSBuildExtensionsPath32)\Microsoft\Portable\$(TargetFrameworkVersion)\Microsoft.Portable.CSharp.targets" };
                }
                else if (this.language is IVisualBasic)
                {
                    return new ProjectImport() { Project = @"$(MSBuildExtensionsPath32)\Microsoft\Portable\$(TargetFrameworkVersion)\Microsoft.Portable.VisualBasic.targets" };
                }
            }
            else
            {
                if (this.language is ICSharp)
                {
                    return new ProjectImport() { Project = @"$(MSBuildExtensionsPath)\Microsoft\WindowsXaml\v$(VisualStudioVersion)\Microsoft.Windows.UI.Xaml.CSharp.targets" };
                }
                else if (this.language is IVisualBasic)
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
                if (this.projectType == WinRTProjectType.UWPLibrary)
                {
                    return "Library";
                }
                else if (this.projectType == WinRTProjectType.UWPApplication)
                {
                    return "AppContainerExe";
                }

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

            if (this.language is ICSharp)
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

            if (IsUWPProject)
            {
                result.Add("WINDOWS_UWP");
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
            bool shouldAddVisualBasicItems = this.language is IVisualBasic && this.projectType != WinRTProjectType.ComponentForUniversal;
            object[] items = new object[GetNumberOfProjectItems(shouldAddVisualBasicItems)];

            int currentItem = 0;

            items[currentItem++] = this.GenerateCommonPropsProjectImportProperty();

            items[currentItem++] = basicProjectProperties;

            object[] configurations = this.GetConfigurations(basicProjectProperties);
            for (int j = 0; j < configurations.Length; j++)
            {
                items[currentItem++] = configurations[j];
            }

            items[currentItem++] = this.CreatePojectReferences(module, basicProjectProperties);

            if (this.projectType == WinRTProjectType.ComponentForUniversal)
            {
                items[currentItem++] = new ProjectItemGroup()
                {
                    TargetPlatform = new ProjectItemGroupTargetPlatform[]
                    {
                        new ProjectItemGroupTargetPlatform() { Include = "WindowsPhoneApp, Version=8.1" },
                        new ProjectItemGroupTargetPlatform() { Include = "Windows, Version=8.1" }
                    }
                };
            }

            items[currentItem++] = this.fileGenContext.GetProjectItemGroup();

            if (this.projectType != WinRTProjectType.ComponentForUniversal)
            {
                items[currentItem++] = this.GetVisualStudioVersionPropertyGroup();
            }

            if (this.projectType == WinRTProjectType.ComponentForWindowsPhone)
            {
                items[currentItem++] = new ProjectPropertyGroup() { Condition = " '$(TargetPlatformIdentifier)' == '' ", TargetPlatformIdentifier = "WindowsPhoneApp" };
            }

            if (shouldAddVisualBasicItems)
            {
                items[currentItem++] = this.GetCompileOptions();
            }

            items[currentItem++] = this.GenerateLanguageTargetsProjectImportProperty();
            
            return items;
        }

        private int GetNumberOfProjectItems(bool shouldAddVisualBasicItems)
        {
            int numberOfItems = 8;
            if (this.projectType == WinRTProjectType.ComponentForWindowsPhone)
            {
                numberOfItems++;
            }

            if (shouldAddVisualBasicItems)
            {
                numberOfItems++;
            }

            return numberOfItems;
        }

        protected override ProjectItemGroup CreatePojectReferences(ModuleDefinition module, ProjectPropertyGroup basicProjectProperties)
        {
            ProjectItemGroup result = base.CreatePojectReferences(module, basicProjectProperties);
            if (IsUWPProject)
            {
                result.None = new ProjectItemGroupNone() { Include = ProjectJsonWriter.ProjectJsonFileName };
            }

            return result;
        }

        protected override ICollection<AssemblyNameReference> FilterDependingOnAssemblies(ICollection<AssemblyNameReference> dependingOnAssemblies)
        {
            ICollection<AssemblyNameReference> result = new List<AssemblyNameReference>();
            foreach (AssemblyNameReference reference in base.FilterDependingOnAssemblies(dependingOnAssemblies))
            {
                if (IsUWPProject)
                {
                    if (NetCoreFrameworkAssemblies.Contains(reference.Name))
                    {
                        this.dependencies.Add(reference.Name, reference.Version.ToString(3));
                        continue;
                    }
                    else if (this.UWPReferenceAssemblies.Contains(reference.Name))
                    {
                        continue;
                    }
                }
                else if (reference.Name == "System.Runtime")
                {
                    continue;
                }

                result.Add(reference);
            }

            return result;
        }

        protected override void WriteModuleAdditionalFiles(ModuleDefinition module)
        {
            if (module.IsMain && IsUWPProject)
            {
                ProjectJsonWriter writer = new ProjectJsonWriter(targetDir, this.dependencies, UAPPlatformIdentifier + this.minInstalledUAPVersion.ToString(3), this.runtimes);
                writer.ExceptionThrown += OnExceptionThrown;
                bool isSuccessfull = writer.WriteProjectJsonFile();
                writer.ExceptionThrown -= OnExceptionThrown;

                this.OnProjectFileCreated(new FileGeneratedInfo(writer.ProjectJsonFilePath, !isSuccessfull));
            }
        }

        protected override bool WriteSolutionFile()
        {
            SolutionWriter solutionWriter =
                new SolutionWriter(this.assembly, this.platform, this.targetDir, this.filePathsService.GetSolutionRelativePath(),
                                   this.modulesToProjectsFilePathsMap, this.modulesProjectsGuids, this.visualStudioVersion,
                                   this.language);
            solutionWriter.WriteSolutionFile();

			return true;
        }

        private void AddAdditionalProjectProperties(ProjectPropertyGroup project)
        {
            project.ProjectTypeGuids = this.GetProjectTypeGuids(this.assembly.MainModule);
            project.MinimumVisualStudioVersion = this.GetMinimumVisualStudioVersion();
            project.TargetPlatformVersion = this.GetTargetPlatformVersion();
            project.TargetFrameworkProfile = this.GetTargetFrameworkProfile();
            if (IsUWPProject)
            {
                project.TargetPlatformMinVersion = this.minInstalledUAPVersion.ToString();
                project.TargetPlatformIdentifier = UAPPlatformIdentifier;
            }

            project.AllowCrossPlatformRetargeting = false;
            project.AllowCrossPlatformRetargetingSpecified = this.projectType == WinRTProjectType.UWPComponent;
        }

        private ProjectPropertyGroup[] GetConfigurations(ProjectPropertyGroup basicProjectProperties)
        {
            ProjectPropertyGroup[] configurations = new ProjectPropertyGroup[2]; // Debug + Release

            configurations[0] = this.CreateConfiguration(basicProjectProperties.Platform.Value, true);
            configurations[1] = this.CreateConfiguration(basicProjectProperties.Platform.Value, false);

            return configurations;
        }

        protected override ProjectPropertyGroup CreateConfiguration(string platform, bool debugConfiguration)
        {
            ProjectPropertyGroup config = base.CreateConfiguration(platform, debugConfiguration);

            config.UseVSHostingProcess = false;
            config.UseVSHostingProcessSpecified = true;
            if (this.IsApplicationProject() && platform == "AnyCPU")
            {
                config.Prefer32Bit = true;
                config.Prefer32BitSpecified = true;
            }
            
            if (this.language is ICSharp)
            {
                config.NoWarn = ";2008";
                config.WarningLevelSpecified = false;
            }
            else if (this.language is IVisualBasic)
            {
                if (debugConfiguration)
                {
                    config.NoConfig = true;
                    config.NoConfigSpecified = true;
                }
                else
                {
                    config.NoStdLib = true;
                    config.NoStdLibSpecified = true;
                    config.NoConfig = true;
                    config.NoConfigSpecified = true;
                }
            }

            if (this.projectType == WinRTProjectType.UWPApplication)
            {
                config.UseDotNetNativeToolchain = !debugConfiguration;
                config.UseDotNetNativeToolchainSpecified = true;
            }

            return config;
        }

        private bool IsApplicationProject()
        {
            return this.projectType == WinRTProjectType.UWPApplication;
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
            else if (this.visualStudioVersion == VisualStudioVersion.VS2015)
            {
                visualStudioProductVersion = "14.0";
            }
            else if (this.visualStudioVersion == VisualStudioVersion.VS2017)
            {
                visualStudioProductVersion = "15.0";
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
            else if (IsUWPProject)
            {
                result += UWPProjectGUID;
            }

            result += ";";
            if (this.language is ICSharp)
            {
                result += ("{" + CSharpGUID + "}");
            }
            else if (this.language is IVisualBasic)
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
            else if (IsUWPProject)
            {
                return "14";
            }

            return null;
        }

        private string GetTargetPlatformVersion()
        {
            if (this.projectType == WinRTProjectType.ComponentForWindows || this.projectType == WinRTProjectType.ComponentForWindowsPhone)
            {
                return "8.1";
            }
            else if (IsUWPProject)
            {
                return this.maxInstalledUAPVersion.ToString();
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
            this.dependencies = new Dictionary<string, string>();
            this.runtimes = new List<string>();

            if (this.IsUWPProject)
            {
                TargetArchitecture architecture = this.assembly.MainModule.GetModuleArchitecture();
                if (architecture == TargetArchitecture.I386)
                {
                    this.runtimes.Add("win10-x86");
                    this.runtimes.Add("win10-x86-aot");
                }
                else if (architecture == TargetArchitecture.AMD64)
                {
                    this.runtimes.Add("win10-x64");
                    this.runtimes.Add("win10-x64-aot");
                }
                else if (architecture == TargetArchitecture.ARMv7)
                {
                    this.runtimes.Add("win10-arm");
                    this.runtimes.Add("win10-arm-aot");
                }
            }

            InitializeInstalledUAPVersions();
        }

        private void InitializeInstalledUAPVersions()
        {
            Version minPossibleVersion = new Version(0, 0, 0, 0);
            Version maxPossibleVersion = new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
            
            Version minVersion = maxPossibleVersion;
            Version maxVersion = minPossibleVersion;
            if (Directory.Exists(SystemInformation.UAP_PLATFORM))
            {
                foreach (string item in Directory.EnumerateDirectories(SystemInformation.UAP_PLATFORM))
                {
                    Version currentVersion;
                    if (Version.TryParse((new DirectoryInfo(item)).Name, out currentVersion))
                    {
                        if (currentVersion < minVersion)
                        {
                            minVersion = currentVersion;
                        }

                        if (currentVersion > maxVersion)
                        {
                            maxVersion = currentVersion;
                        }
                    }
                }
            }

            this.minInstalledUAPVersion = minVersion != maxPossibleVersion ? minVersion : DefaultUAPVersion;
            this.maxInstalledUAPVersion = maxVersion != minPossibleVersion ? maxVersion : DefaultUAPVersion;
        }
    }
}
