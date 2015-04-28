using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil.Extensions;
using AssemblyPathName = System.Collections.Generic.KeyValuePair<string, string>;

namespace Mono.Cecil.AssemblyResolver
{
    internal class AssemblyPathResolver
    {
        private readonly AssemblyPathResolverCache pathRepository;
        private readonly ReaderParameters readerParameters;

        public AssemblyPathResolver(AssemblyPathResolverCache pathRepository, ReaderParameters readerParameters)
        {
            this.pathRepository = pathRepository;
            this.readerParameters = readerParameters;
        }

        public void AddToAssemblyCache(string filePath, TargetArchitecture architecture)
        {
            AssemblyName assemblyName;
            if (TryGetAssemblyNameDefinition(filePath, true, architecture, out assemblyName))
            {
                TargetPlatform platform = GetTargetPlatform(filePath);
                if (!pathRepository.AssemblyParts.ContainsKey(filePath))
                {
                    pathRepository.AssemblyParts.Add(filePath, platform);
                }
                if (!pathRepository.AssemblyPathName.ContainsKey(assemblyName.FullName))
                {
                    CheckFileExistence(assemblyName, filePath, true, false);

                    RemoveFromUnresolvedCache(assemblyName.FullName);
                }
            }
        }

        internal string GetAssemblyPath(AssemblyName assemblyName)
        {
            if (IsFailedAssembly(assemblyName.FullName))
            {
                return string.Empty;
            }
            string filePath = string.Empty;

            if (string.IsNullOrEmpty(filePath))
            {
                IEnumerable<string> filePaths = GetAssemblyPaths(assemblyName);

                filePath = filePaths.FirstOrDefault() ?? string.Empty;

                if (string.IsNullOrEmpty(filePath))
                {
                    foreach (string currentFilePath in filePaths)
                    {
                        if (!string.IsNullOrEmpty(currentFilePath))
                        {
                            filePath = currentFilePath;
                            break;
                        }
                    }
                }

                if (CheckFoundedAssembly(assemblyName, filePath))
                {
                    return filePath;
                }
            }
            return filePath;
        }

        public TargetPlatform GetTargetPlatform(string assemliyFilePath)
        {
            if (string.IsNullOrEmpty(assemliyFilePath))
            {
                return TargetPlatform.None;
            }
            if (pathRepository.AssemblyParts.ContainsKey(assemliyFilePath))
            {
                return pathRepository.AssemblyParts[assemliyFilePath];
            }
            else
            {
                ModuleDefinition moduleDef = ModuleDefinition.ReadModule(assemliyFilePath, readerParameters);
				AssemblyNameReference msCorlib = moduleDef.AssemblyReferences.FirstOrDefault(a => a.Name == "mscorlib");

				if (msCorlib == null)
				{
					msCorlib = moduleDef.AssemblyReferences.FirstOrDefault(x => x.Name == "System.Runtime");
					if (msCorlib != null)
					{
						return TargetPlatform.WinRT;
					}
					// the next line is only to keep the old functionality
					msCorlib = moduleDef.Assembly.Name;
				}

                if (moduleDef.Assembly != null && moduleDef.Assembly.Name.IsWindowsRuntime || msCorlib.IsFakeMscorlibReference())
                {
                    return TargetPlatform.WinRT;
                }

                AssemblyName assemblyName = new AssemblyName(msCorlib.Name,
                                                    msCorlib.FullName,
                                                    msCorlib.Version,
                                                    msCorlib.PublicKeyToken,
                                                    Path.GetDirectoryName(assemliyFilePath)) { TargetArchitecture = moduleDef.GetModuleArchitecture() };
                IEnumerable<string> foundPaths = GetAssemblyPaths(assemblyName);

                return GetTargetPlatform(foundPaths.FirstOrDefault());
            }
        }

        public bool ArePublicKeyEquals(byte[] publicKeyToken1, byte[] publicKeyToken2)
        {
            if (publicKeyToken1 == null && publicKeyToken2 == null)
            {
                return true;
            }
            if (publicKeyToken1 != null && publicKeyToken2 != null)
            {
                return publicKeyToken1.SequenceEqual(publicKeyToken2);
            }
            return false;
        }

        public bool CheckFileExistence(AssemblyName assemblyName, string searchPattern, bool caching, bool checkForBaseDir, bool checkForArchitectPlatfrom = true)
        {
            AssemblyName assemblyNameFromStorage;
            if (TryGetAssemblyNameDefinition(searchPattern, caching, assemblyName.TargetArchitecture, out assemblyNameFromStorage, checkForArchitectPlatfrom))
            {
                var areEquals = AreVersionEquals(assemblyNameFromStorage.Version, assemblyName.Version)
                                && ArePublicKeyEquals(assemblyNameFromStorage.PublicKeyToken, assemblyName.PublicKeyToken)
                                && assemblyName.TargetArchitecture.CanReference(assemblyNameFromStorage.TargetArchitecture)
                                && (!checkForBaseDir || AreDefaultDirEqual(assemblyName, assemblyNameFromStorage));
                if (areEquals && caching)
                {
                    pathRepository.AssemblyPathName.Add(assemblyName.FullName, searchPattern);
                    if (!pathRepository.AssemblyPathArchitecture.ContainsKey(searchPattern))
                    {
                        TargetArchitecture architecture = AssemblyDefinition.ReadAssembly(searchPattern, readerParameters).MainModule.GetModuleArchitecture();
                        pathRepository.AssemblyPathArchitecture.Add(new KeyValuePair<string, TargetArchitecture>(searchPattern, architecture));
                    }
                }
                return areEquals;
            }
            return false;
        }

        public void ClearCache()
        {
            pathRepository.Clear();
        }

        private bool CheckFoundedAssembly(AssemblyName assemblyName, string filePath)
        {
            bool checkFileExistence = CheckFileExistence(assemblyName, filePath, false, false);
            return checkFileExistence;
        }

        internal bool TryGetAssemblyPathsFromCache(AssemblyName sourceAssemblyName, out IEnumerable<string> filePaths)
        {
            filePaths = Enumerable.Empty<string>();

            if (pathRepository.AssemblyPathName.ContainsKey(sourceAssemblyName.FullName))
            {
                List<string> targets = pathRepository.AssemblyPathName.Where(a => a.Key == sourceAssemblyName.FullName)
                                                        .Select(a => a.Value)
                                                        .ToList();

                if (sourceAssemblyName.HasDefaultDir && targets.Any(d => Path.GetDirectoryName(d) == sourceAssemblyName.DefaultDir))
                {
                    filePaths = targets.Where(d => Path.GetDirectoryName(d) == sourceAssemblyName.DefaultDir);

                    return true;
                }
                else
                {
                    string targetAssembly = pathRepository.AssemblyPathName
                                                             .Where(a => pathRepository.AssemblyPathArchitecture.ContainsKey(a.Value) && pathRepository.AssemblyPathArchitecture[a.Value].CanReference(sourceAssemblyName.TargetArchitecture))
                                                             .FirstOrDefault(a => a.Key == sourceAssemblyName.FullName).Value;
                    if (targetAssembly != null)
                    {
                        filePaths = new string[] { targetAssembly };

                        return true;
                    }
                }
            }
            return false;
        }

        public IEnumerable<string> GetAssemblyPaths(AssemblyName sourceAssemblyName)
        {
            IEnumerable<string> results;

            if (TryGetAssemblyPathsFromCache(sourceAssemblyName, out results))
            {
                return results;
            }
            if (IsFailedAssembly(sourceAssemblyName.FullName))
            {
                return Enumerable.Empty<string>();
            }
            var platforms = new List<TargetPlatform>
            {
                TargetPlatform.CLR_4,
                TargetPlatform.CLR_2_3,
                TargetPlatform.Silverlight,
                TargetPlatform.WindowsPhone,
                TargetPlatform.WindowsCE,
                TargetPlatform.CLR_1,
                TargetPlatform.WinRT
            };
            var result = new List<string>();
            foreach (TargetPlatform platform in platforms)
            {
                IEnumerable<string> assemblyLocations = GetAssemblyLocationsByPlatform(sourceAssemblyName, platform);

                result.AddRange(assemblyLocations);
            }
            return result;
        }

        private IEnumerable<string> GetAssemblyLocationsByPlatform(AssemblyName assemblyName, TargetPlatform runtime)
        {
            var result = new List<string>();
            switch (runtime)
            {
                case TargetPlatform.CLR_1:
                    result.AddRange(ResolveClr1(assemblyName));
                    break;
                case TargetPlatform.CLR_2_3:
                case TargetPlatform.CLR_4:
                    result.AddRange(ResolveClr(assemblyName, runtime));
                    break;

                case TargetPlatform.WindowsCE:
                    result.AddRange(ResolveCompact(assemblyName));
                    break;

                case TargetPlatform.Silverlight:
                case TargetPlatform.WindowsPhone:
                    result.AddRange(ResolveSilverlightPaths(assemblyName));
                    result.AddRange(ResolveWP(assemblyName));
                    break;

                case TargetPlatform.WinRT:
                    result.AddRange(ResolveWinRTMetadata(assemblyName));
                    break;
            }
            AddPartCacheResult(result, runtime);

            return result;
        }

        private void AddPartCacheResult(IEnumerable<string> resultPaths, TargetPlatform runtime)
        {
            foreach (string resultPath in resultPaths)
            {
                if (!string.IsNullOrEmpty(resultPath) && !pathRepository.AssemblyParts.ContainsKey(resultPath))
                {
                    pathRepository.AssemblyParts[resultPath] = runtime;
                }
            }
        }

        private IEnumerable<string> ResolveSilverlightPaths(AssemblyName assemblyName)
        {
            List<string> result = new List<string>();

            string runtime = ResolveSilverlightRuntimePath(assemblyName, SystemInformation.SILVERLIGHT_RUNTIME);

            string runtime64 = ResolveSilverlightRuntimePath(assemblyName, SystemInformation.SILVERLIGHT_RUNTIME_64);

            IEnumerable<string> @default = ResolveSilverlightPath(assemblyName, SystemInformation.SILVERLIGHT_DEFAULT);

            IEnumerable<string> sdk = ResolveSilverlightPath(assemblyName, SystemInformation.SILVERLIGHT_SDK);

            if (string.IsNullOrEmpty(runtime) == false)
            {
                result.Add(runtime);
            }
            result.AddRange(@default);

            result.AddRange(sdk);
            /*Telerik Authorship*/
            if (string.IsNullOrEmpty(runtime64) == false)
            {
                result.Add(runtime64);
            }
            return result;
        }

        private string ResolveSilverlightRuntimePath(AssemblyName assemblyName, string path)
        {
            string searchPattern = string.Format(path, assemblyName.Name);
            if (CheckFileExistence(assemblyName, searchPattern, true, true))
            {
                return searchPattern;
            }
            return string.Empty;
        }

        private IEnumerable<string> ResolveSilverlightPath(AssemblyName assemblyName, string path)
        {
            foreach (var version in assemblyName.SupportedVersions(TargetPlatform.Silverlight))
            {
                string searchPattern = string.Format(path, SystemInformation.ProgramFilesX86, version, assemblyName.Name);
                if (CheckFileExistence(assemblyName, searchPattern, true, true))
                {
                    yield return searchPattern;
                }
            }
        }

        private IEnumerable<string> ResolveCompact(AssemblyName assemblyName)
        {
            foreach (var ver in assemblyName.SupportedVersions(TargetPlatform.WindowsCE))
            {
                var searchPattern = string.Format(SystemInformation.COMPACT_FRAMEWORK, SystemInformation.ProgramFilesX86, ver, assemblyName.Name);
                if (CheckFileExistence(assemblyName, searchPattern, true, false))
                {
                    yield return searchPattern;
                }
            }
        }

        private IEnumerable<string> ResolveWP(AssemblyName assemblyName)
        {
            foreach (var version in assemblyName.SupportedVersions(TargetPlatform.WindowsPhone))
            {
                string windowsPhoneDir = string.Format(@"{0}\Reference Assemblies\Microsoft\Framework\Silverlight\{1}\Profile\",
                                                       SystemInformation.ProgramFilesX86,
                                                       version);
                if (Directory.Exists(windowsPhoneDir))
                {
                    foreach (string dirVersions in Directory.GetDirectories(windowsPhoneDir, "WindowsPhone*."))
                    {
                        string searchPattern = string.Format("{0}\\{1}.dll", dirVersions, assemblyName.Name);
                        if (CheckFileExistence(assemblyName, searchPattern, true, true))
                        {
                            yield return searchPattern;
                        }
                    }
                }
            }
        }

        private IEnumerable<string> ResolveClr(AssemblyName assemblyName, TargetPlatform clrRuntime)
        {
            var result = new List<string>();

            string versionMajor = string.Format("v{0}*", (clrRuntime == TargetPlatform.CLR_4 ? 4 : 2));

            var targetDirectories = new List<string>();
            if (Directory.Exists(SystemInformation.CLR_Default_32))
            {
                targetDirectories.AddRange(Directory.GetDirectories(SystemInformation.CLR_Default_32, versionMajor));
            }
            if (Directory.Exists(SystemInformation.CLR_Default_64))
            {
                targetDirectories.AddRange(Directory.GetDirectories(SystemInformation.CLR_Default_64, versionMajor));
            }
            foreach (var dir in targetDirectories)
            {
                foreach (string extension in SystemInformation.ResolvableExtensions)
                {
                    string filePath = dir + "\\" + assemblyName.Name + extension;
                    if (CheckFileExistence(assemblyName, filePath, true, false))
                    {
                        result.Add(filePath);
                    }
                }
            }
            var searchPattern = clrRuntime == TargetPlatform.CLR_4 ? SystemInformation.CLR_4 : SystemInformation.CLR;

            foreach (var targetRuntime in assemblyName.SupportedVersions(clrRuntime))
            {
                foreach (var extension in SystemInformation.ResolvableExtensions)
                {
                    string filePath = string.Format(searchPattern,
                                                    SystemInformation.WindowsPath,
                                                    targetRuntime,
                                                    assemblyName.Name,
                                                    assemblyName.ParentDirectory(clrRuntime),
                                                    assemblyName.Name,
                                                    extension);
                    if (CheckFileExistence(assemblyName, filePath, true, false))
                    {
                        result.Add(filePath);
                    }
                }
            }
            return result;
        }

        private IEnumerable<string> ResolveClr1(AssemblyName assemblyName)
        {
            string path = Path.Combine(SystemInformation.WindowsPath, "Microsoft.NET\\Framework");
            if (assemblyName.Version.Major != 1)
            {
                yield break;
            }

            foreach (string extension in SystemInformation.ResolvableExtensions)
            {
                string[] files = Directory.GetFiles(path, assemblyName.Name + extension, SearchOption.AllDirectories);
                foreach (string filePath in files)
                {
                    if (CheckFileExistence(assemblyName, filePath, true, false))
                    {
                        yield return filePath;
                    }
                }
            }
        }

        private IEnumerable<string> ResolveWinRTMetadata(AssemblyName assemblyName)
        {
            string filePath = string.Format(@"{0}\{1}.winmd", SystemInformation.WINRT_METADATA, assemblyName.Name);
            if (CheckFileExistence(assemblyName, filePath, true, false))
            {
                yield return filePath;
            }

            if (!Directory.Exists(SystemInformation.WINDOWS_WINMD_LOCATION))
            {
                yield break;
            }

            foreach (string foundFile in Directory.GetFiles(SystemInformation.WINDOWS_WINMD_LOCATION, assemblyName.Name + ".winmd", SearchOption.AllDirectories))
            {
                if (CheckFileExistence(assemblyName, foundFile, true, false))
                {
                    yield return foundFile;
                }
            }
        }

        private bool AreDefaultDirEqual(AssemblyName assemblyName, AssemblyName assemblyNameFromStorage)
        {
            if (!assemblyName.HasDefaultDir && !assemblyNameFromStorage.HasDefaultDir)
            {
                return true;
            }
            if (assemblyName.HasDefaultDir && assemblyNameFromStorage.HasDefaultDir)
            {
                if (File.Exists(Path.Combine(assemblyName.DefaultDir, assemblyName.Name + ".dll"))
                    && File.Exists(Path.Combine(assemblyNameFromStorage.DefaultDir, assemblyNameFromStorage.Name + ".dll")))
                {
                    return assemblyName.DefaultDir == assemblyNameFromStorage.DefaultDir;
                }
                return true;
            }
            return true;
        }

        private bool AreVersionEquals(Version version1, Version version2)
        {
            if (IsZero(version1) && IsZero(version2))
            {
                return true;
            }
            else if (!IsZero(version1) && !IsZero(version2))
            {
                return Version.Equals(version1, version2);
            }
            return false;
        }

        private bool IsZero(Version version)
        {
            return version == null || (version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0);
        }

        private bool TryGetAssemblyNameDefinition(string assemblyFilePath,
                                                         bool caching,
                                                         TargetArchitecture architecture,
                                                         out AssemblyName assemblyName,
                                                         bool checkForArchitectPlatfrom = true)
        {
            assemblyName = null;
            if (pathRepository.AssemblyNameDefinition.ContainsKey(assemblyFilePath))
            {
                assemblyName = pathRepository.AssemblyNameDefinition[assemblyFilePath];
                if (!checkForArchitectPlatfrom)
                {
                    return true;
                }
                else if (assemblyName.TargetArchitecture == architecture)
                {
                    return true;
                }
            }
            if ((caching || assemblyName == null) && File.Exists(assemblyFilePath))
            {
                var moduleDef = ModuleDefinition.ReadModule(assemblyFilePath, readerParameters);
                if (moduleDef != null && moduleDef.Assembly != null)
                {
                    AssemblyDefinition assemblyDef = moduleDef.Assembly;
                    assemblyName = new AssemblyName(moduleDef.Name,
                                                    assemblyDef.FullName,
                                                    assemblyDef.Name.Version,
                                                    assemblyDef.Name.PublicKeyToken,
                                                    Path.GetDirectoryName(assemblyFilePath)) { TargetArchitecture = moduleDef.GetModuleArchitecture() };
                    pathRepository.AssemblyNameDefinition[assemblyFilePath] = assemblyName;
                    return true;
                }
            }
            return false;
        }

        internal void RemoveFromAssemblyCache(string fileName)
        {
            int index = pathRepository.AssemblyPathName.FindIndex(p => p.Value == fileName);
            if (index != -1)
            {
                pathRepository.AssemblyPathName.RemoveAt(index);
            }
            if (pathRepository.AssemblyParts.ContainsKey(fileName))
            {
                pathRepository.AssemblyParts.Remove(fileName);
            }
            if (pathRepository.AssemblyNameDefinition.ContainsKey(fileName))
            {
                pathRepository.AssemblyNameDefinition.Remove(fileName);
            }
            if (pathRepository.AssemblyPathArchitecture.ContainsKey(fileName))
            {
                pathRepository.AssemblyPathArchitecture.Remove(fileName);
            }
        }

        internal void AddToAssemblyPathNameCache(AssemblyName assemblyName, string filePath)
        {
            pathRepository.AssemblyPathName.Add(assemblyName.FullName, filePath);

            if (!pathRepository.AssemblyPathArchitecture.ContainsKey(filePath))
            {
                pathRepository.AssemblyPathArchitecture.Add(new KeyValuePair<string, TargetArchitecture>(filePath, assemblyName.TargetArchitecture));
            }
        }

        #region failed cache
        internal void AddToUnresolvedCache(string fullName)
        {
            if (!this.pathRepository.AssemblyFaildedResolverCache.Contains(fullName))
            {
                this.pathRepository.AssemblyFaildedResolverCache.Add(fullName);
            }
        }

        internal void RemoveFromUnresolvedCache(string fullName)
        {
            if (pathRepository.AssemblyFaildedResolverCache.Contains(fullName))
            {
                pathRepository.AssemblyFaildedResolverCache.Remove(fullName);
            }
        }

        internal bool IsFailedAssembly(string fullName)
        {
            return pathRepository.AssemblyFaildedResolverCache.Contains(fullName);
        }

        internal IClonableCollection<string> GetAssemblyFailedResolvedCache()
        {
            return pathRepository.AssemblyFaildedResolverCache;
        }

        internal void SetFailedAssemblyCache(IList<string> list)
        {
            foreach (string file in list)
            {
                AddToUnresolvedCache(file);
            }
        }

        internal void ClearAssemblyFailedResolverCache()
        {
            pathRepository.AssemblyFaildedResolverCache.Clear();
        }
        #endregion
    }

    public static class Extensions
    {
        internal static bool ContainsKey(this IList<AssemblyPathName> collection, string key)
        {
            return collection.Any(a => a.Key == key);
        }

        internal static void Add(this IList<AssemblyPathName> collection, string key, string value)
        {
            collection.Add(new AssemblyPathName(key, value));
        }

        public static bool IsReferenceAssembly(this ModuleDefinition moduleDef)
        {
            if (moduleDef == null || moduleDef.Assembly == null || moduleDef.Assembly.CustomAttributes == null)
            {
                return false;
            }
            foreach (var attribute in moduleDef.Assembly.CustomAttributes)
            {
                if (attribute.AttributeType.Name == "ReferenceAssemblyAttribute" && attribute.AttributeType.Namespace == "System.Runtime.CompilerServices")
                {
                    return true;
                }
            }
            return false;
        }
    }
}

