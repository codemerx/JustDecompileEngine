using JustDecompile.Tools.MSBuildProjectBuilder.Contracts.FileManagers;
using JustDecompile.Tools.MSBuildProjectBuilder.NetCore;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.JustDecompiler.External;
using Telerik.JustDecompiler.Languages;
using Mono.Collections.Generic;
using Telerik.JustDecompiler.External.Interfaces;

namespace JustDecompile.Tools.MSBuildProjectBuilder
{
	public class TestNetCoreProjectBuilder : NetCoreProjectBuilder
	{
		public TestNetCoreProjectBuilder(string assemblyPath, string targetPath, ILanguage language, INetCoreProjectManager projectFileManager,
			Dictionary<ModuleDefinition, Guid> modulesProjectGuids, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2017,
			ProjectGenerationSettings projectGenerationSettings = null)
            : base(assemblyPath, targetPath, language, new DecompilationPreferences() { RenameInvalidMembers = true, WriteDocumentation = true, WriteFullNames = false },
				  null, projectFileManager, modulesProjectGuids, visualStudioVersion, projectGenerationSettings)
		{
			this.exceptionFormater = TestCaseExceptionFormatter.Instance;
			this.currentAssemblyResolver.ClearCache();
		}

		public TestNetCoreProjectBuilder(string assemblyPath, AssemblyDefinition assembly,
			Dictionary<ModuleDefinition, Collection<TypeDefinition>> userDefinedTypes,
			Dictionary<ModuleDefinition, Collection<Resource>> resources, string targetPath, ILanguage language,
			INetCoreProjectManager projectFileManager, Dictionary<ModuleDefinition, Guid> modulesProjectGuids,
			VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2017, ProjectGenerationSettings projectGenerationSettings = null,
			IProjectGenerationNotifier projectNotifier = null) 
			: base(assemblyPath, assembly, userDefinedTypes, resources, targetPath, language, new DecompilationPreferences() { RenameInvalidMembers = true, WriteDocumentation = true, WriteFullNames = false, WriteLargeNumbersInHex = true },
				  projectFileManager, modulesProjectGuids, visualStudioVersion, projectGenerationSettings, projectNotifier)
		{
			this.exceptionFormater = TestCaseExceptionFormatter.Instance;
			this.currentAssemblyResolver.ClearCache();
		}
	}
}
