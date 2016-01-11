using Mono.Cecil;
using Mono.Cecil.AssemblyResolver;
using System.Collections.Generic;

namespace Telerik.JustDecompiler
{
    public class ThreadSafeWeakAssemblyResolver : WeakAssemblyResolver
    {
        private object directoriesAccessLock = new object();

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
    }
}
