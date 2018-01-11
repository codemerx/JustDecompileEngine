using System;
using System.Collections.Generic;
using Mono.Cecil;
using JustDecompile.Tools.MSBuildProjectBuilder.Contracts.FileManagers;
using Telerik.JustDecompiler.External;
using Telerik.JustDecompiler.Languages;

namespace JustDecompile.Tools.MSBuildProjectBuilder
{
	public class TestMSBuildProjectBuilder : MSBuildProjectBuilder
	{
        public TestMSBuildProjectBuilder(string assemblyPath, AssemblyDefinition assembly, string targetPath, ILanguage language, IMsBuildProjectManager projectFileManager, Dictionary<ModuleDefinition, Guid> modulesProjectGuids, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010, ProjectGenerationSettings projectGenerationSettings = null)
            : base(assemblyPath, assembly, null, null, targetPath, language, new DecompilationPreferences() { RenameInvalidMembers = true, WriteDocumentation = true, WriteFullNames = false }, projectFileManager, modulesProjectGuids, visualStudioVersion, projectGenerationSettings)
		{
			this.exceptionFormater = TestCaseExceptionFormatter.Instance;
			this.currentAssemblyResolver.ClearCache();
		}
	}
}
