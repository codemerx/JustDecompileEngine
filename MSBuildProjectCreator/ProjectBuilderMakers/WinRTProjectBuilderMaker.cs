using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustDecompile.EngineInfrastructure;
using Mono.Cecil;
using Mono.Collections.Generic;
using Telerik.JustDecompiler.External;
using Telerik.JustDecompiler.External.Interfaces;
using Telerik.JustDecompiler.Languages;
using JustDecompile.Tools.MSBuildProjectBuilder.Contracts.FileManagers;
using JustDecompile.Tools.MSBuildProjectBuilder.ProjectFileManagers;
using Mono.Cecil.AssemblyResolver;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectBuilderMakers
{
	public class WinRTProjectBuilderMaker : MsBuildProjectBuilderMaker
	{
		protected IWinRTProjectManager winRTProjectFileManager;
		private WinRTProjectType projectType;

		public WinRTProjectBuilderMaker(string assemblyPath, AssemblyDefinition assembly,
			Dictionary<ModuleDefinition, Collection<TypeDefinition>> userDefinedTypes,
			Dictionary<ModuleDefinition, Collection<Resource>> resources,
			string targetPath, ILanguage language, IFrameworkResolver frameworkResolver, ITargetPlatformResolver targetPlatformResolver, IDecompilationPreferences preferences,
			IAssemblyInfoService assemblyInfoService, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010, ProjectGenerationSettings settings = null) 
			: base(assemblyPath, assembly, userDefinedTypes, resources, targetPath, language, frameworkResolver, targetPlatformResolver, preferences, assemblyInfoService, visualStudioVersion, settings)
		{
			this.projectType = WinRTProjectTypeDetector.GetProjectType(this.assembly);
		}

		public WinRTProjectBuilderMaker(string assemblyPath, string targetPath, ILanguage language,
			ITargetPlatformResolver targetPlatformResolver, IDecompilationPreferences preferences, IFileGenerationNotifier notifier, IAssemblyInfoService assemblyInfoService,
			VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010, ProjectGenerationSettings settings = null,
			IProjectGenerationNotifier projectNotifier = null) 
			: base(assemblyPath, targetPath, language, null, targetPlatformResolver, preferences, notifier, assemblyInfoService, visualStudioVersion, settings, projectNotifier)
		{
			this.projectType = WinRTProjectTypeDetector.GetProjectType(this.assembly);
		}

		public override void InitializeProjectFileManager()
		{
			this.winRTProjectFileManager =
				new WinRTProjectFileManager(this.assembly, this.assemblyInfo, this.language, this.visualStudioVersion, this.namespaceHierarchyTree,
				this.modulesProjectsGuids, this.projectType);
		}

		public override BaseProjectBuilder GetBuilder()
		{
			return new WinRTProjectBuilder(this.assemblyPath, this.assembly, this.userDefinedTypes, this.resources, this.targetPath, this.language,
				this.preferences, this.winRTProjectFileManager, this.modulesProjectsGuids, this.visualStudioVersion, this.projectGenerationSettings);
		}
	}
}
