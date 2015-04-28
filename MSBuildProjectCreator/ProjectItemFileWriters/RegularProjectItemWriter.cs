using System;
using System.Collections.Generic;
using System.IO;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectItemFileWriters
{
    class RegularProjectItemWriter : BaseProjectItemFileWriter
    {
        private readonly string relativePath;
        private readonly List<ProjectItemGroupCompile> normalProjectItemGroup;

        public RegularProjectItemWriter(string projectRootDirectory, string relativeFilePath, List<ProjectItemGroupCompile> normalProjectItemGroup)
        {
            this.relativePath = relativeFilePath;
            this.normalProjectItemGroup = normalProjectItemGroup;
            this.fullPath = Path.Combine(projectRootDirectory, this.relativePath);
        }
    
        public override void GenerateProjectItems()
        {
            normalProjectItemGroup.Add(new ProjectItemGroupCompile() { Include = relativePath });
        }
    }
}
