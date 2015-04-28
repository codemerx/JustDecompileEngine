using System;
using System.Collections.Generic;
using System.IO;
using JustDecompile.Tools.MSBuildProjectBuilder.FilePathsServices;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectItemFileWriters
{
    class ResXDesignerWriter : BaseProjectItemFileWriter
    {
        private readonly string relativeResourcePath;
        private readonly string relativeDesignerPath;
        private readonly List<ProjectItemGroupCompile> designerItemGroup;
        private readonly List<ProjectItemGroupEmbeddedResource> resourceItemGroup;

        public ResXDesignerWriter(string projectRootDirectory, string relativeResourcePath,
            List<ProjectItemGroupCompile> designerItemGroup,
            List<ProjectItemGroupEmbeddedResource> resourceItemGroup,
			IFilePathsService filePathsService)
        {
            this.designerItemGroup = designerItemGroup;
            this.resourceItemGroup = resourceItemGroup;
            this.relativeResourcePath = relativeResourcePath;
			this.relativeDesignerPath = Path.Combine(Path.GetDirectoryName(relativeResourcePath), filePathsService.GetResourceDesignerRelativePath(Path.GetFileNameWithoutExtension(relativeResourcePath)));
            this.fullPath = Path.Combine(projectRootDirectory, this.relativeDesignerPath);
        }

        public override void GenerateProjectItems()
        {
            ProjectItemGroupCompile sourceEntry = new ProjectItemGroupCompile();
            sourceEntry.Include = relativeDesignerPath;
            sourceEntry.DependentUpon = Path.GetFileName(relativeResourcePath);
            sourceEntry.DesignTimeSharedInput = true;
            sourceEntry.AutoGen = true;
            designerItemGroup.Add(sourceEntry);

            ProjectItemGroupEmbeddedResource resourceEntry = new ProjectItemGroupEmbeddedResource();
            resourceEntry.Include = relativeResourcePath;
            resourceEntry.Generator = "ResXFileCodeGenerator";
            resourceEntry.LastGenOutput = Path.GetFileName(relativeDesignerPath);
            resourceItemGroup.Add(resourceEntry);
        }
    }
}
