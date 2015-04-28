using Mono.Cecil;
using Telerik.JustDecompiler.External.Interfaces;

namespace JustDecompile.EngineInfrastructure
{
    public interface IAssemblyInfoService
    {
        AssemblyInfo GetAssemblyInfo(AssemblyDefinition assembly, IFrameworkResolver frameworkResolver);
    }
}