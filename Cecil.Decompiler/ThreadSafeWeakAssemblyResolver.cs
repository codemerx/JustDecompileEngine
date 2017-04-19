using Mono.Cecil;
using Mono.Cecil.AssemblyResolver;
using System.Collections.Generic;

namespace Telerik.JustDecompiler
{
    public class ThreadSafeWeakAssemblyResolver : WeakAssemblyResolver
    {
        private object directoriesAccessLock = new object();
        private object resolvedAssembliesAccessLock = new object();

        public ThreadSafeWeakAssemblyResolver(AssemblyPathResolverCache cache)
            : base(cache)
        {
        }

        public override void AddSearchDirectory(string directory)
        {
            lock (this.directoriesAccessLock)
            {
                base.AddSearchDirectory(directory);
            }
        }

        protected override void ClearDirectoriesCache()
        {
            lock (this.directoriesAccessLock)
            {
                base.ClearDirectoriesCache();
            }
        }

        protected override IEnumerable<DirectoryAssemblyInfo> GetDirectoryAssemblies()
        {
            lock (this.directoriesAccessLock)
            {
                return base.GetDirectoryAssemblies();
            }
        }

        public override string[] GetSearchDirectories()
        {
            lock (this.directoriesAccessLock)
            {
                return base.GetSearchDirectories();
            }
        }

        public override void RemoveSearchDirectory(string directory)
        {
            lock (this.directoriesAccessLock)
            {
                base.RemoveSearchDirectory(directory);
            }
        }

        protected override bool TryGetResolvedAssembly(AssemblyStrongNameExtended assemblyKey, out List<AssemblyDefinition> assemblyList)
        {
            lock (this.resolvedAssembliesAccessLock)
            {
                return base.TryGetResolvedAssembly(assemblyKey, out assemblyList);
            }
        }

        protected override void ClearResolvedAssembliesCache()
        {
            lock (this.resolvedAssembliesAccessLock)
            {
                base.ClearResolvedAssembliesCache();
            }
        }

        protected override void RemoveFromResolvedAssemblies(AssemblyStrongNameExtended assemblyKey)
        {
            lock (this.resolvedAssembliesAccessLock)
            {
                base.RemoveFromResolvedAssemblies(assemblyKey);
            }
        }

        protected override void AddToResolvedAssembliesInternal(AssemblyStrongNameExtended assemblyKey, List<AssemblyDefinition> assemblyList)
        {
            lock (this.resolvedAssembliesAccessLock)
            {
                base.AddToResolvedAssembliesInternal(assemblyKey, assemblyList);
            }
        }
    }
}
