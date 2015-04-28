using System;
using System.Collections.Generic;
using System.IO;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectItemFileWriters
{
    class WinFormsItemWriter : BaseProjectItemFileWriter
    {
        private readonly string relativeResourcePath;
        private readonly string relativeWinFormPath;
        private readonly List<ProjectItemGroupCompile> winFormsItemGroup;
        private readonly List<ProjectItemGroupEmbeddedResource> resourceItemGroup;

        public WinFormsItemWriter(string projectRootDirectory, string relativeResourcePath, string sourceExtension,
            List<ProjectItemGroupCompile> winFormsItemGroup,
            List<ProjectItemGroupEmbeddedResource> resourceItemGroup)
        {
            this.relativeResourcePath = relativeResourcePath;
            this.relativeWinFormPath = Path.ChangeExtension(relativeResourcePath, sourceExtension);
            this.fullPath = Path.Combine(projectRootDirectory, relativeWinFormPath);
            this.winFormsItemGroup = winFormsItemGroup;
            this.resourceItemGroup = resourceItemGroup;
        }

        public override void GenerateProjectItems()
        {
            ProjectItemGroupCompile winFormEntry = new ProjectItemGroupCompile();
            winFormEntry.Include = relativeWinFormPath;
            winFormEntry.SubType = "Form";
            winFormsItemGroup.Add(winFormEntry);

            ProjectItemGroupEmbeddedResource resourceEntry = new ProjectItemGroupEmbeddedResource();
            resourceEntry.Include = relativeResourcePath;
            resourceEntry.DependentUpon = Path.GetFileName(relativeWinFormPath);
            resourceItemGroup.Add(resourceEntry);
        }
    }
}
