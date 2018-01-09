using JustDecompile.EngineInfrastructure;
using Mono.Cecil.AssemblyResolver;
using System;
using Telerik.JustDecompiler.External;
using Telerik.JustDecompiler.External.Interfaces;
using Telerik.JustDecompiler.Languages;

namespace JustDecompile.Tools.MSBuildProjectBuilder
{
	public class TestMSBuildProjectBuilder : MSBuildProjectBuilder
	{
        public TestMSBuildProjectBuilder(string assemblyPath, string targetPath, ILanguage language, VisualStudioVersion visualStudioVersion = VisualStudioVersion.VS2010, ProjectGenerationSettings projectGenerationSettings = null)
            : base(assemblyPath, targetPath, language, new TestsFrameworkVersionResolver(), new DecompilationPreferences() { RenameInvalidMembers = true, WriteDocumentation = true, WriteFullNames = false }, null, NoCacheAssemblyInfoService.Instance, TargetPlatformResolver.Instance, visualStudioVersion, projectGenerationSettings)
		{
			this.exceptionFormater = TestCaseExceptionFormatter.Instance;
			this.currentAssemblyResolver.ClearCache();
		}

        class TestsFrameworkVersionResolver : IFrameworkResolver
        {
            public FrameworkVersion GetDefaultFallbackFramework4Version()
            {
                return FrameworkVersion.v4_0;
            }
        }
	}
}
