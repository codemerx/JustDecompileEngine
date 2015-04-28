using System;
using System.Collections.Generic;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectItemFileWriters
{
    class AppDefinitionItemWriter : BaseXamlFileWriter
    {
        public AppDefinitionItemWriter(string projectRootDirectory, string relativeXamlPath, string sourceExtension,
            List<ProjectItemGroupCompile> codeItemGroup,
            List<object> xamlItemGroup)
            :base(projectRootDirectory, relativeXamlPath, sourceExtension, codeItemGroup, xamlItemGroup)
        {
        }

        protected override object GetXamlEntry()
        {
            ProjectItemGroupApplicationDefinition appDefItem = new ProjectItemGroupApplicationDefinition();
            appDefItem.Include = this.relativeXamlPath;
            appDefItem.Generator = "MSBuild:Compile";
            appDefItem.SubType = "Designer";
            return appDefItem;
        }
    }
}
