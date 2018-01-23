using System;
using JustDecompile.EngineInfrastructure;
using Telerik.JustDecompiler.External;
using Telerik.JustDecompiler.Languages;
using JustDecompile.Tools.MSBuildProjectBuilder.Contracts.FileManagers;
using JustDecompile.Tools.MSBuildProjectBuilder.ProjectFileManagers;
using Mono.Cecil.AssemblyResolver;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectBuilderMakers
{
	public class TestNetCoreProjectBuilderMaker : BaseProjectBuilderMaker
	{
		private INetCoreProjectManager projectFileManager;
		private readonly FrameworkVersion defaultFrameworkVersion;

		public TestNetCoreProjectBuilderMaker(string assemblyPath, string targetPath, ILanguage language,
			IAssemblyInfoService assemblyInfoService, FrameworkVersion defaultFrameworkVersion,
			ProjectGenerationSettings projectGenerationSettings = null, IProjectGenerationNotifier projectNotifier = null) 
			: base(assemblyPath, targetPath, language, null, TargetPlatformResolver.Instance, null,
				  null, assemblyInfoService, VisualStudioVersion.VS2017, projectGenerationSettings, projectNotifier)
		{
			this.defaultFrameworkVersion = defaultFrameworkVersion;
		}

		public override BaseProjectBuilder GetBuilder()
		{
			return new TestNetCoreProjectBuilder(this.assemblyPath, this.assembly, null, null, this.targetPath, this.language, this.projectFileManager, this.modulesProjectsGuids, this.visualStudioVersion, this.projectGenerationSettings);
		}

		public override void InitializeProjectFileManager()
		{
			this.projectFileManager = new TestNetCoreProjectFileManager(this.assembly, this.modulesProjectsGuids, defaultFrameworkVersion);
		}
	}
}
