using System;
//using JustDecompile.EngineInfrastructure;
using Mono.Cecil;
using System.IO;
using Telerik.JustDecompiler.Languages.CSharp;
using Telerik.JustDecompiler.Languages;
using System.Collections.Generic;
using JustDecompile.Tools.MSBuildProjectBuilder.FilePathsServices;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectItemFileWriters
{
    class ProjectItemWriterFactory
    {
        private readonly FileGenerationContext fileGenContext;
        private readonly string sourceExtension;
        private readonly AssemblyDefinition assembly;
		private object fileNamesCollisionsLock = new object();
		private readonly IFilePathsService filePathsService;
		private readonly Dictionary<TypeDefinition, string> typeToPathMap;

        public ProjectItemWriterFactory(AssemblyDefinition thisAssembly, Mono.Collections.Generic.Collection<TypeDefinition> userDefinedTypes, 
			FileGenerationContext fileGenContext, IFilePathsService filePathsService, string sourceExtension)
        {
            this.assembly = thisAssembly;
            this.fileGenContext = fileGenContext;
			this.filePathsService = filePathsService;
			this.typeToPathMap = filePathsService.GetTypesToFilePathsMap();
            this.sourceExtension = sourceExtension;
        }

        public IProjectItemFileWriter GetProjectItemWriter(TypeDefinition type)
        {
            string relativeResourcePath;
            if(fileGenContext.ResourceDesignerMap.TryGetValue(type.FullName, out relativeResourcePath) ||
                IsVBResourceType(type, out relativeResourcePath))
            {
                if (type.BaseType != null && type.BaseType.Namespace == "System.Windows.Forms" && type.BaseType.Name == "Form")
                {
                    return new WinFormsItemWriter(fileGenContext.TargetDirectory,
                                                  relativeResourcePath,
                                                  sourceExtension,
                                                  fileGenContext.WinFormCodeEntries,
                                                  fileGenContext.WinFormResXEntries);
                }
                else
                {
                    return new ResXDesignerWriter(fileGenContext.TargetDirectory,
                                                  relativeResourcePath,
                                                  fileGenContext.NormalCodeEntries,
                                                  fileGenContext.ResXEntries,
												  filePathsService);
                }
            }

            string relativeXamlPath;
            if(fileGenContext.XamlFullNameToRelativePathMap.TryGetValue(type.FullName, out relativeXamlPath))
            {
                if(assembly.EntryPoint != null && assembly.EntryPoint.DeclaringType == type)
                {
                    return new AppDefinitionItemWriter(fileGenContext.TargetDirectory,
                                                relativeXamlPath,
                                                sourceExtension,
                                                fileGenContext.NormalCodeEntries,
                                                fileGenContext.XamlFileEntries);
                }
                else
                {
                    return new XamlPageItemWriter(fileGenContext.TargetDirectory,
                                                relativeXamlPath,
                                                sourceExtension,
                                                fileGenContext.NormalCodeEntries,
                                                fileGenContext.XamlFileEntries);
                }
            }

			string friendlyFilePath;

			if (type.FullName == "Properties.AssemblyInfo")
			{
				friendlyFilePath = filePathsService.GetAssemblyInfoRelativePath();
			}
			else
			{
				lock (this.fileNamesCollisionsLock)
				{
					friendlyFilePath = this.typeToPathMap[type];
				}
			}

            return new RegularProjectItemWriter(fileGenContext.TargetDirectory,
											   friendlyFilePath,
                                               fileGenContext.NormalCodeEntries);
        }

        private bool IsVBResourceType(TypeDefinition type, out string relativeResourcePath)
        {
            relativeResourcePath = null;
            string resourceName;
            return Utilities.IsVBResourceType(type, out resourceName) && fileGenContext.ResourceDesignerMap.TryGetValue(resourceName, out relativeResourcePath);
        }
    }
}
