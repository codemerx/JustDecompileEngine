using System;
using System.Collections.Generic;
using Mono.Cecil;
using JustDecompile.EngineInfrastructure;
using Telerik.JustDecompiler;
using Telerik.JustDecompiler.External;
using Telerik.JustDecompiler.External.Interfaces;
using Telerik.JustDecompiler.Languages;
using Mono.Cecil.AssemblyResolver;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectBuilderMakers
{
	public abstract class BaseProjectBuilderMaker
	{
		protected readonly string assemblyPath;
		protected readonly AssemblyDefinition assembly;
		protected readonly Dictionary<ModuleDefinition, Mono.Collections.Generic.Collection<TypeDefinition>> userDefinedTypes;
		protected readonly Dictionary<ModuleDefinition, Mono.Collections.Generic.Collection<Resource>> resources;
		protected readonly string targetPath;
		protected readonly ILanguage language;
		protected readonly IFrameworkResolver frameworkResolver;
		protected readonly ITargetPlatformResolver targetPlatformResolver;
		protected readonly IDecompilationPreferences preferences;
		protected readonly IFileGenerationNotifier notifier;
		protected readonly IAssemblyInfoService assemblyInfoService;
		protected readonly VisualStudioVersion visualStudioVersion;
		protected readonly ProjectGenerationSettings projectGenerationSettings;
		protected readonly IProjectGenerationNotifier projectNotifier;
		protected readonly Dictionary<ModuleDefinition, Guid> modulesProjectsGuids;
		protected readonly AssemblyInfo assemblyInfo;
		protected readonly IAssemblyResolver assemblyResolver;

		public BaseProjectBuilderMaker(string assemblyPath, AssemblyDefinition assembly,
			Dictionary<ModuleDefinition, Mono.Collections.Generic.Collection<TypeDefinition>> userDefinedTypes,
			Dictionary<ModuleDefinition, Mono.Collections.Generic.Collection<Resource>> resources,
			string targetPath, ILanguage language, IFrameworkResolver frameworkResolver, ITargetPlatformResolver targetPlatformResolver, IDecompilationPreferences preferences, IAssemblyInfoService assemblyInfoService,
			VisualStudioVersion visualStudioVersion, ProjectGenerationSettings projectGenerationSettings = null, IProjectGenerationNotifier projectNotifier = null)
		{
			this.assemblyResolver = assembly.MainModule.AssemblyResolver;

			this.assembly = assembly;
			this.assemblyInfoService = assemblyInfoService;
			this.userDefinedTypes = userDefinedTypes;
			this.resources = resources;
			this.targetPath = targetPath;
			this.targetPlatformResolver = targetPlatformResolver;
			this.language = language;
			this.frameworkResolver = frameworkResolver;
			this.preferences = preferences;
			this.visualStudioVersion = visualStudioVersion;
			this.projectGenerationSettings = projectGenerationSettings;
			this.projectNotifier = projectNotifier;
			this.assemblyInfo = this.assemblyInfoService.GetAssemblyInfo(this.assembly, this.frameworkResolver, this.targetPlatformResolver);
			this.modulesProjectsGuids = new Dictionary<ModuleDefinition, Guid>();
		}

		public BaseProjectBuilderMaker(string assemblyPath, string targetPath, ILanguage language,
			IFrameworkResolver frameworkResolver, ITargetPlatformResolver targetPlatformResolver, IDecompilationPreferences preferences, IFileGenerationNotifier notifier,
			IAssemblyInfoService assemblyInfoService, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010,
			ProjectGenerationSettings projectGenerationSettings = null, IProjectGenerationNotifier projectNotifier = null)
		{
			this.assemblyResolver = new WeakAssemblyResolver(GlobalAssemblyResolver.CurrentAssemblyPathCache);

			this.assemblyPath = assemblyPath;
			this.targetPath = targetPath;
			this.language = language;
			this.frameworkResolver = frameworkResolver;
			this.targetPlatformResolver = targetPlatformResolver;
			this.preferences = preferences;
			this.visualStudioVersion = visualStudioVersion;
			this.notifier = notifier;
			this.assemblyInfoService = assemblyInfoService;
			this.projectGenerationSettings = projectGenerationSettings;
			this.projectNotifier = projectNotifier;

			ReaderParameters readerParameters = new ReaderParameters(this.assemblyResolver);
			this.assembly = this.assemblyResolver.LoadAssemblyDefinition(assemblyPath, readerParameters, loadPdb: true);
			this.assemblyInfo = this.assemblyInfoService.GetAssemblyInfo(this.assembly, this.frameworkResolver, this.targetPlatformResolver);
			this.modulesProjectsGuids = new Dictionary<ModuleDefinition, Guid>();

		}

		public abstract void InitializeProjectFileManager();

		public abstract BaseProjectBuilder GetBuilder();
	}
}
