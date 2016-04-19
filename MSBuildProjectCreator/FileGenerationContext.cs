using System;
using System.Collections.Generic;
using Telerik.JustDecompiler.Common.NamespaceHierarchy;

namespace JustDecompile.Tools.MSBuildProjectBuilder
{
    public class FileGenerationContext
    {
        public ProjectItemGroupCompile AssemblyInfoFileEntry { get; set; }
        public ProjectItemGroupNone AppConfigFileEntry { get; set; }
        public string TargetDirectory { get; private set; }
        public NamespaceHierarchyTree NamespacesTree { get; private set; }

        public Dictionary<string, string> ResourceDesignerMap { get; private set; }
        public Dictionary<string, string> XamlFullNameToRelativePathMap { get; private set; }

        public List<ProjectItemGroupCompile> NormalCodeEntries { get; private set; }
        public List<ProjectItemGroupCompile> WinFormCodeEntries { get; private set; }
        public List<ProjectItemGroupEmbeddedResource> WinFormResXEntries { get; private set; }
        public List<ProjectItemGroupEmbeddedResource> ResXEntries { get; private set; }
        public List<object> XamlFileEntries { get; private set; }
        public List<ProjectItemGroupEmbeddedResource> OtherEmbeddedResources { get; private set; }
        public List<ProjectItemGroupResource> OtherXamlResources { get; private set; }

        public FileGenerationContext(string targetDir, NamespaceHierarchyTree namespaceHierarchyTree)
        {
            TargetDirectory = targetDir;
            NamespacesTree = namespaceHierarchyTree;
            NormalCodeEntries = new List<ProjectItemGroupCompile>();
            WinFormCodeEntries = new List<ProjectItemGroupCompile>();
            WinFormResXEntries = new List<ProjectItemGroupEmbeddedResource>();
            ResXEntries = new List<ProjectItemGroupEmbeddedResource>();
            XamlFileEntries = new List<object>();
            ResourceDesignerMap = new Dictionary<string, string>();
            XamlFullNameToRelativePathMap = new Dictionary<string, string>();
            OtherEmbeddedResources = new List<ProjectItemGroupEmbeddedResource>();
            OtherXamlResources = new List<ProjectItemGroupResource>();
        }

        public ProjectItemGroup GetProjectItemGroup()
        {
            ProjectItemGroup projectItemGroup = new ProjectItemGroup();

            List<ProjectItemGroupEmbeddedResource> embeddedResources = new List<ProjectItemGroupEmbeddedResource>(WinFormResXEntries);
            embeddedResources.AddRange(ResXEntries);
            embeddedResources.AddRange(OtherEmbeddedResources);
            projectItemGroup.EmbeddedResource = embeddedResources.ToArray();

            List<ProjectItemGroupCompile> compileFiles = new List<ProjectItemGroupCompile>();
            if(AssemblyInfoFileEntry != null)
            {
                compileFiles.Add(AssemblyInfoFileEntry);
            }
            compileFiles.AddRange(NormalCodeEntries);
            compileFiles.AddRange(WinFormCodeEntries);
            projectItemGroup.Compile = compileFiles.ToArray();

            if (this.AppConfigFileEntry != null)
            {
                projectItemGroup.None = this.AppConfigFileEntry;
            }

            List<ProjectItemGroupPage> xamlPageList = new List<ProjectItemGroupPage>(XamlFileEntries.Count);
            for (int i = 0; i < XamlFileEntries.Count; i++)
            {
                if (XamlFileEntries[i] is ProjectItemGroupPage)
                {
                    xamlPageList.Add((ProjectItemGroupPage)XamlFileEntries[i]);
                }
                else
                {
                    projectItemGroup.ApplicationDefinition = (ProjectItemGroupApplicationDefinition)XamlFileEntries[i];
                }
            }
            projectItemGroup.Page = xamlPageList.ToArray();

            projectItemGroup.Resource = OtherXamlResources.ToArray();

            return projectItemGroup;
        }
    }
}
