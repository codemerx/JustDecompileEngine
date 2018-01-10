using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Extensions;
using Telerik.JustDecompiler.Common.NamespaceHierarchy;
using Telerik.JustDecompiler.External.Interfaces;
using Telerik.JustDecompiler.Languages;
using JustDecompile.EngineInfrastructure;
using JustDecompile.Tools.MSBuildProjectBuilder.Contracts.FileManagers;
using JustDecompile.Tools.MSBuildProjectBuilder.ProjectFileManagers;
using Telerik.JustDecompiler.External;
using Mono.Cecil.AssemblyResolver;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectBuilderMakers
{
	public class MsBuildProjectBuilderMaker : BaseProjectBuilderMaker
	{
		protected readonly NamespaceHierarchyTree namespaceHierarchyTree;
		private IMsBuildProjectManager projectFileManager;

		public MsBuildProjectBuilderMaker(string assemblyPath, AssemblyDefinition assembly, Dictionary<ModuleDefinition, Mono.Collections.Generic.Collection<TypeDefinition>> userDefinedTypes,
			Dictionary<ModuleDefinition, Mono.Collections.Generic.Collection<Resource>> resources, string targetPath, ILanguage language,
			IFrameworkResolver frameworkResolver, ITargetPlatformResolver targetPlatformResolver, IDecompilationPreferences preferences, IAssemblyInfoService assemblyInfoService,
			VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010, ProjectGenerationSettings settings = null)
			: base(assemblyPath, assembly, userDefinedTypes, resources, targetPath, language, frameworkResolver, targetPlatformResolver, preferences, assemblyInfoService, visualStudioVersion, settings)
		{
			this.namespaceHierarchyTree = assembly.BuildNamespaceHierarchyTree();
		}

		public MsBuildProjectBuilderMaker(string assemblyPath, string targetPath, ILanguage language,
			IFrameworkResolver frameworkResolver, ITargetPlatformResolver targetPlatformResolver, IDecompilationPreferences preferences, IFileGenerationNotifier notifier,
			IAssemblyInfoService assemblyInfoService, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010,
			ProjectGenerationSettings settings = null, IProjectGenerationNotifier projectNotifier = null)
			: base(assemblyPath, targetPath, language, frameworkResolver, targetPlatformResolver, preferences, notifier, assemblyInfoService, visualStudioVersion, settings, projectNotifier)
		{
			this.namespaceHierarchyTree = this.assembly.BuildNamespaceHierarchyTree();
		}

		public override BaseProjectBuilder GetBuilder()
		{
			return new MSBuildProjectBuilder(this.assemblyPath, this.assembly, this.userDefinedTypes, this.resources, this.targetPath, this.language,
				this.preferences, this.projectFileManager, this.modulesProjectsGuids, this.visualStudioVersion, this.projectGenerationSettings);
		}

		public override void InitializeProjectFileManager()
		{
			this.projectFileManager = 
				new MsBuildProjectFileManager(this.assembly, this.assemblyInfo, this.visualStudioVersion, this.modulesProjectsGuids,
					this.language, this.namespaceHierarchyTree);
		}
	}
}
