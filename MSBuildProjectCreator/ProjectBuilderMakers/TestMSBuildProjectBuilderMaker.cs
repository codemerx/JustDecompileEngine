using System;
using System.Collections.Generic;
using JustDecompile.EngineInfrastructure;
using Mono.Cecil;
using Mono.Collections.Generic;
using Telerik.JustDecompiler.External;
using Telerik.JustDecompiler.External.Interfaces;
using Telerik.JustDecompiler.Languages;
using Mono.Cecil.AssemblyResolver;
using JustDecompile.Tools.MSBuildProjectBuilder.ProjectFileManagers;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectBuilderMakers
{
	public class TestMSBuildProjectBuilderMaker : MsBuildProjectBuilderMaker
	{
		public TestMSBuildProjectBuilderMaker(string assemblyPath, AssemblyDefinition assembly, Dictionary<ModuleDefinition, Collection<TypeDefinition>> userDefinedTypes, Dictionary<ModuleDefinition, Collection<Resource>> resources, string targetPath, ILanguage language, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010, ProjectGenerationSettings settings = null) 
			: base(assemblyPath, assembly, userDefinedTypes, resources, targetPath, language, new TestsFrameworkVersionResolver(), TargetPlatformResolver.Instance, new DecompilationPreferences() { RenameInvalidMembers = true, WriteDocumentation = true, WriteFullNames = false },
				  NoCacheAssemblyInfoService.Instance, visualStudioVersion, settings)
		{
		}

		public TestMSBuildProjectBuilderMaker(string assemblyPath, string targetPath, ILanguage language, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010, ProjectGenerationSettings settings = null, IProjectGenerationNotifier projectNotifier = null) 
			: base(assemblyPath, targetPath, language, new TestsFrameworkVersionResolver(), TargetPlatformResolver.Instance, new DecompilationPreferences() { RenameInvalidMembers = true, WriteDocumentation = true, WriteFullNames = false },
				  null, NoCacheAssemblyInfoService.Instance, visualStudioVersion, settings, projectNotifier)
		{
		}

		public override BaseProjectBuilder GetBuilder()
		{
			return new TestMSBuildProjectBuilder(this.assemblyPath, this.assembly, this.targetPath, this.language, this.projectFileManager, this.modulesProjectsGuids, this.visualStudioVersion, this.projectGenerationSettings);
		}

		public override void InitializeProjectFileManager()
		{
			this.projectFileManager =
				new MsBuildProjectFileManager(this.assembly, this.assemblyInfo, this.visualStudioVersion, this.modulesProjectsGuids,
					this.language, this.namespaceHierarchyTree);
		}

		internal class TestsFrameworkVersionResolver : IFrameworkResolver
		{
			public FrameworkVersion GetDefaultFallbackFramework4Version()
			{
				return FrameworkVersion.v4_0;
			}
		}
	}
}
