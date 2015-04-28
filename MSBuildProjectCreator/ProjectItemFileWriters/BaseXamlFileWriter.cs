using System;
using System.Collections.Generic;
using System.IO;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectItemFileWriters
{
    abstract class BaseXamlFileWriter : BaseProjectItemFileWriter
    {
        protected readonly string relativeXamlPath;
        private readonly List<object> xamlItemGroup;
        private readonly string relativeCodePath;
        private readonly List<ProjectItemGroupCompile> codeItemGroup;

        public BaseXamlFileWriter(string projectRootDirectory, string relativeXamlPath, string sourceExtension,
            List<ProjectItemGroupCompile> codeItemGroup,
            List<object> xamlItemGroup)
        {
            this.relativeXamlPath = relativeXamlPath;
            this.relativeCodePath = relativeXamlPath + sourceExtension;
            this.fullPath = Path.Combine(projectRootDirectory, relativeCodePath);
            this.codeItemGroup = codeItemGroup;
            this.xamlItemGroup = xamlItemGroup;
        }

        public override void GenerateProjectItems()
        {
            ProjectItemGroupCompile codeEntry = new ProjectItemGroupCompile();
            codeEntry.Include = relativeCodePath;
            codeEntry.SubType = "Code";
            codeEntry.DependentUpon = Path.GetFileName(relativeXamlPath);
            codeItemGroup.Add(codeEntry);

            xamlItemGroup.Add(GetXamlEntry());
        }

        protected abstract object GetXamlEntry();
    }
}
