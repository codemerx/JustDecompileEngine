using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Telerik.JustDecompiler.External.Interfaces;
using Telerik.JustDecompiler.Languages;
using JustDecompile.EngineInfrastructure;
using JustDecompile.Tools.MSBuildProjectBuilder.Contracts.FileManagers;
using JustDecompile.Tools.MSBuildProjectBuilder.NetCore;
using JustDecompile.Tools.MSBuildProjectBuilder.ProjectFileManagers;
using Mono.Cecil.AssemblyResolver;
using Telerik.JustDecompiler.External;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectBuilderMakers
{
	public class NetCoreProjectBuilderMaker : BaseProjectBuilderMaker
	{
		private INetCoreProjectManager netCoreProjectFileManager;

		public NetCoreProjectBuilderMaker(string assemblyPath, AssemblyDefinition assembly,
			Dictionary<ModuleDefinition, Mono.Collections.Generic.Collection<TypeDefinition>> userDefinedTypes,
			Dictionary<ModuleDefinition, Mono.Collections.Generic.Collection<Resource>> resources,
			string targetPath, ILanguage language, IFrameworkResolver frameworkResolver, ITargetPlatformResolver targetPlatformResolver, IDecompilationPreferences preferences,
			IAssemblyInfoService assemblyInfoService, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2017, ProjectGenerationSettings settings = null)
			: base(assemblyPath, assembly, userDefinedTypes, resources, targetPath, language, frameworkResolver, targetPlatformResolver, preferences,
				  assemblyInfoService, visualStudioVersion, settings)
		{
		}

		public NetCoreProjectBuilderMaker(string assemblyPath, string targetPath, ILanguage language, IFrameworkResolver frameworkResolver,
			ITargetPlatformResolver targetPlatformResolver, IDecompilationPreferences preferences, IFileGenerationNotifier notifier,
			IAssemblyInfoService assemblyInfoService, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010,
			ProjectGenerationSettings projectGenerationSettings = null, IProjectGenerationNotifier projectNotifier = null) 
			: base(assemblyPath, targetPath, language, frameworkResolver, targetPlatformResolver, preferences, notifier, assemblyInfoService,
				  visualStudioVersion, projectGenerationSettings, projectNotifier)
		{
		}

		public override BaseProjectBuilder GetBuilder()
		{
			return new NetCoreProjectBuilder(this.assemblyPath, this.assembly, this.userDefinedTypes, this.resources, this.targetPath, this.language, this.preferences,
				this.netCoreProjectFileManager, this.modulesProjectsGuids, this.visualStudioVersion, this.projectGenerationSettings);
		}

		public override void InitializeProjectFileManager()
		{
			this.netCoreProjectFileManager = new NetCoreProjectFileManager(this.assembly, this.assemblyInfo, this.modulesProjectsGuids);
		}
	}
}
