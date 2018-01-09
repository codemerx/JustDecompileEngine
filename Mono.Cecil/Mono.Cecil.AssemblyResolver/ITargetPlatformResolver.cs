
namespace Mono.Cecil.AssemblyResolver
{
	/*Telerik Authorship*/
	public interface ITargetPlatformResolver
	{
		TargetPlatform GetTargetPlatform(ModuleDefinition module);

		TargetPlatform GetTargetPlatform(string targetFrameworkAttributeValue);
	}
}
