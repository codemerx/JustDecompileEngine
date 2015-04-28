using System;
using System.Collections.Generic;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectItemFileWriters
{
    class XamlPageItemWriter : BaseXamlFileWriter
    {

        public XamlPageItemWriter(string projectRootDirectory, string relativeXamlPath, string sourceExtension,
            List<ProjectItemGroupCompile> codeItemGroup,
            List<object> xamlItemGroup)
            :base(projectRootDirectory, relativeXamlPath, sourceExtension, codeItemGroup, xamlItemGroup)
        {
        }

        protected override object GetXamlEntry()
        {
            ProjectItemGroupPage pageItem = new ProjectItemGroupPage();
            pageItem.Include = this.relativeXamlPath;
            pageItem.Generator = "MSBuild:Compile";
            pageItem.SubType = "Designer";
            return pageItem;
        }
    }
}
