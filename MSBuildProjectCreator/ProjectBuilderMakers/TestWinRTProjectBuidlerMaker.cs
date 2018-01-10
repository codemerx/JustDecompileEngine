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
using Mono.Cecil.AssemblyResolver;

namespace JustDecompile.Tools.MSBuildProjectBuilder.ProjectBuilderMakers
{
	public class TestWinRTProjectBuidlerMaker : WinRTProjectBuilderMaker
	{
		public TestWinRTProjectBuidlerMaker(string assemblyPath, string targetPath, ILanguage language, ITargetPlatformResolver targetPlatformResolver, IDecompilationPreferences preferences, IFileGenerationNotifier notifier, IAssemblyInfoService assemblyInfoService, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010, ProjectGenerationSettings settings = null, IProjectGenerationNotifier projectNotifier = null) 
			: base(assemblyPath, targetPath, language, targetPlatformResolver, preferences, notifier, assemblyInfoService, visualStudioVersion, settings, projectNotifier)
		{
		}

		public TestWinRTProjectBuidlerMaker(string assemblyPath, AssemblyDefinition assembly, Dictionary<ModuleDefinition, Collection<TypeDefinition>> userDefinedTypes, Dictionary<ModuleDefinition, Collection<Resource>> resources, string targetPath, ILanguage language, IFrameworkResolver frameworkResolver, ITargetPlatformResolver targetPlatformResolver, IDecompilationPreferences preferences, IAssemblyInfoService assemblyInfoService, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010, ProjectGenerationSettings settings = null) 
			: base(assemblyPath, assembly, userDefinedTypes, resources, targetPath, language, frameworkResolver, targetPlatformResolver, preferences, assemblyInfoService, visualStudioVersion, settings)
		{
		}
	}
}
