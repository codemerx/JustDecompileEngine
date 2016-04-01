using JustDecompile.EngineInfrastructure.AssemblyLocators;
using Mono.Cecil;
using Mono.Cecil.AssemblyResolver;
using System;
using System.IO;
using System.Runtime.Versioning;
using Telerik.JustDecompiler.External;
using Telerik.JustDecompiler.External.Interfaces;

namespace JustDecompile.EngineInfrastructure
{
    public class NoCacheAssemblyInfoService : IAssemblyInfoService
    {
        private static NoCacheAssemblyInfoService instance = null;

        protected NoCacheAssemblyInfoService()
        {
        }

        public static NoCacheAssemblyInfoService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NoCacheAssemblyInfoService();
                }

                return instance;
            }
        }

        public AssemblyInfo GetAssemblyInfo(AssemblyDefinition assembly, IFrameworkResolver frameworkResolver)
        {
            AssemblyInfo assemblyInfo = new AssemblyInfo();

            AddModulesFrameworkVersions(assemblyInfo, assembly, frameworkResolver);
            AddAssemblyTypes(assemblyInfo, assembly);

            return assemblyInfo;
        }

        protected virtual void AddModulesFrameworkVersions(AssemblyInfo assemblyInfo, AssemblyDefinition assembly, IFrameworkResolver frameworkResolver)
        {
            foreach (ModuleDefinition module in assembly.Modules)
            {
                FrameworkVersion frameworkVersion = GetFrameworkVersionForModule(module, frameworkResolver);
                assemblyInfo.ModulesFrameworkVersions.Add(module, frameworkVersion);
            }
        }

        protected FrameworkVersion GetFrameworkVersionForModule(ModuleDefinition module, IFrameworkResolver frameworkResolver)
        {
            //TODO: handle Silverlight/WinPhone projects
            TargetPlatform platform = module.AssemblyResolver.GetTargetPlatform(module.FilePath);
            switch (platform)
            {
                case TargetPlatform.CLR_1:
                    return FrameworkVersion.v1_1;
                case TargetPlatform.CLR_2:
                    return FrameworkVersion.v2_0;
                case TargetPlatform.CLR_2_3:
                case TargetPlatform.CLR_3_5:
                    return FrameworkVersion.v3_5;
                case TargetPlatform.CLR_4:
                    return GetFramework4Version(module, frameworkResolver);
                case TargetPlatform.WinRT:
                    return GetWinRTFrameworkVersion(module);
                case TargetPlatform.Silverlight:
                    return FrameworkVersion.Silverlight;
                case TargetPlatform.WindowsCE:
                    return FrameworkVersion.WindowsCE;
                case TargetPlatform.WindowsPhone:
                    return FrameworkVersion.WindowsPhone;
                default:
                    return FrameworkVersion.Unknown;
            }
        }

        protected virtual FrameworkVersion GetFramework4Version(ModuleDefinition module, IFrameworkResolver frameworkResolver)
        {
            FrameworkVersion frameworkVersion;
            if (!TryDetectFramework4Upgrade(module, out frameworkVersion))
            {
                frameworkVersion = frameworkResolver.GetDefaultFallbackFramework4Version();
            }

            return frameworkVersion;
        }

        protected bool TryDetectFramework4Upgrade(ModuleDefinition module, out FrameworkVersion frameworkVersion)
        {
            frameworkVersion = FrameworkVersion.Unknown;
            if (module.IsMain)
            {
                FrameworkName frameworkName;
                if (TryGetTargetFrameworkName(module.Assembly, out frameworkName) &&
                    TryParseFramework4Name(frameworkName.Version.ToString(), out frameworkVersion))
                {
                    return true;
                }
                else
                {
                    bool isInFrameworkDir;
                    if (IsFramework4Assembly(module.Assembly, out isInFrameworkDir))
                    {
                        if (isInFrameworkDir)
                        {
                            frameworkVersion = Framework4VersionResolver.GetInstalledFramework4Version();
                        }
                        else
                        {
                            frameworkVersion = Framework4VersionResolver.GetFrameworkVersionByFileVersion(module.Assembly.MainModule.FilePath);
                        }

                        if (frameworkVersion != FrameworkVersion.Unknown)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool TryParseFramework4Name(string frameworkVersionAsString, out FrameworkVersion frameworkVersion)
        {
            frameworkVersion = FrameworkVersion.Unknown;

            switch (frameworkVersionAsString)
            {
                case "4.0":
                    frameworkVersion = FrameworkVersion.v4_0;
                    break;
                case "4.5":
                    frameworkVersion = FrameworkVersion.v4_5;
                    break;
                case "4.5.1":
                    frameworkVersion = FrameworkVersion.v4_5_1;
                    break;
                case "4.5.2":
                    frameworkVersion = FrameworkVersion.v4_5_2;
                    break;
                case "4.6":
                    frameworkVersion = FrameworkVersion.v4_6;
                    break;
                case "4.6.1":
                    frameworkVersion = FrameworkVersion.v4_6_1;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private bool IsFramework4Assembly(AssemblyDefinition assembly, out bool isInFrameworkDir)
        {
            if (IsInFrameworkDir(assembly.MainModule.FilePath))
            {
                isInFrameworkDir = true;
                return true;
            }
            else
            {
                isInFrameworkDir = false;
            }

            IFrameworkAssemblyLocator assemblyLocator = FrameworkAssembly4xLocatorFactory.Instance(assembly.MainModule.Architecture);

            string assemblyFileName = Path.GetFileName(assembly.MainModule.FilePath);

            foreach (string filePath in assemblyLocator.Assemblies)
            {
                if (Path.GetFileName(filePath) == assemblyFileName)
                {
                    // check the public key token
                    ReaderParameters parameters = new ReaderParameters(assembly.MainModule.AssemblyResolver);

                    AssemblyDefinition listAssembly = AssemblyDefinition.ReadAssembly(filePath, parameters);

                    if (CompareByteArrays(listAssembly.Name.PublicKey, assembly.Name.PublicKey))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsInFrameworkDir(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath).ToLower();
            string framework32Directory = Path.Combine(SystemInformation.CLR_Default_32, "v4.0.30319").ToLower();
            string framework64Directory = Path.Combine(SystemInformation.CLR_Default_64, "v4.0.30319").ToLower();
            if (directory.StartsWith(framework32Directory) || directory.StartsWith(framework64Directory))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1 == null ^ array2 == null)
            {
                return false;
            }
            if (array1 == null && array2 == null)
            {
                return true;
            }
            if (array1.Length != array2.Length)
            {
                return false;
            }
            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        private bool TryGetTargetFrameworkName(AssemblyDefinition assembly, out FrameworkName frameworkName)
        {
            frameworkName = null;
            if (assembly.TargetFrameworkAttributeValue != null)
            {
                try
                {
                    frameworkName = new FrameworkName(assembly.TargetFrameworkAttributeValue);
                }
                catch (ArgumentException)
                {
                    /// The constructor throws exception if the version string is incorrectly formated.
                    return false;
                }

                return true;
            }

            return false;
        }

        private void AddAssemblyTypes(AssemblyInfo assemblyInfo, AssemblyDefinition assembly)
        {
            foreach (AssemblyNameReference reference in assembly.MainModule.AssemblyReferences)
            {
                if (reference.Name == "PresentationFramework")
                {
                    assemblyInfo.AssemblyTypes |= AssemblyTypes.WPF;
                }
                else if (reference.Name == "System.Windows.Forms")
                {
                    assemblyInfo.AssemblyTypes |= AssemblyTypes.WinForms;
                }
                else if (reference.Name == "System.Web.Mvc")
                {
                    assemblyInfo.AssemblyTypes |= AssemblyTypes.MVC;
                }
                else if (reference.Name == "Windows")
                {
                    assemblyInfo.AssemblyTypes |= AssemblyTypes.Windows8Application;
                }
                else if (reference.Name == "Windows.Foundation.UniversalApiContract")
                {
                    assemblyInfo.AssemblyTypes |= AssemblyTypes.UniversalWindows;
                }
                else if (reference.Name == "Mono.Android")
                {
                    assemblyInfo.AssemblyTypes |= AssemblyTypes.XamarinAndroid;
                }
                else if (reference.Name == "Xamarin.iOS" || reference.Name == "monotouch")
                {
                    assemblyInfo.AssemblyTypes |= AssemblyTypes.XamarinIOS;
                }
            }
        }

        private FrameworkVersion GetWinRTFrameworkVersion(ModuleDefinition module)
        {
            FrameworkName frameworkName;
            if (this.TryGetTargetFrameworkName(module.Assembly, out frameworkName))
            {
                if (frameworkName.Identifier == ".NETPortable" && frameworkName.Version == new Version(4, 6))
                {
                    return FrameworkVersion.NetPortableV4_6;
                }
                else if (frameworkName.Identifier == ".NETCore")
                {
                    if (frameworkName.Version == new Version(4, 5))
                    {
                        return FrameworkVersion.NetCoreV4_5;
                    }
                    else if (frameworkName.Version == new Version(4, 5, 1))
                    {
                        return FrameworkVersion.NetCoreV4_5_1;
                    }
                    else if (frameworkName.Version == new Version(5, 0))
                    {
                        return FrameworkVersion.NetCoreV5_0;
                    }
                }
            }

            return FrameworkVersion.WinRT;
        }
    }
}