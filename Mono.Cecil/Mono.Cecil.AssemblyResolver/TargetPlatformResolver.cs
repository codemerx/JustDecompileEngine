using System;
using System.Linq;
using System.Runtime.Versioning;

namespace Mono.Cecil.AssemblyResolver
{
	/*Telerik Authorship*/
	public class TargetPlatformResolver : ITargetPlatformResolver
	{
		private static ITargetPlatformResolver instance;
		private static readonly Version DefaultAssemblyVersion = new Version(0, 0, 0, 0);

		public static ITargetPlatformResolver Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new TargetPlatformResolver();
				}

				return instance;
			}
		}

		public TargetPlatform GetTargetPlatform(string targetFrameworkAttributeValue)
		{
			FrameworkName targetFrameworkAttribute = new FrameworkName(targetFrameworkAttributeValue);

			if (targetFrameworkAttribute.Identifier == ".NETCoreApp")
			{
				return TargetPlatform.NetCore;
			}
			else if (targetFrameworkAttribute.Identifier == ".NETFramework" && targetFrameworkAttribute.Version.Major == 4)
			{
				return TargetPlatform.CLR_4;
			}
			else if (targetFrameworkAttribute.Identifier == "Silverlight")
			{
				if (targetFrameworkAttribute.Profile == "WindowsPhone")
				{
					return TargetPlatform.WindowsPhone;
				}

				return TargetPlatform.Silverlight;
			}
			else if (targetFrameworkAttribute.Identifier == "MonoAndroid" || targetFrameworkAttribute.Identifier == "Xamarin.iOS")
			{
				return TargetPlatform.Xamarin;
			}
			else if (targetFrameworkAttribute.Identifier == "WindowsPhoneApp")
			{
				return TargetPlatform.WinRT;
			}

			return TargetPlatform.None;
		}

		public TargetPlatform GetTargetPlatform(ModuleDefinition module)
		{
			AssemblyNameReference systemRuntime = module.AssemblyReferences.FirstOrDefault(x => x.Name == "System.Runtime");

			if (systemRuntime != null && systemRuntime.Version != DefaultAssemblyVersion)
			{
				if (systemRuntime.Version.Major == 4 && (systemRuntime.Version.Minor == 1 || systemRuntime.Version.Minor == 2))
				{
					return TargetPlatform.NetCore;
				}
				else
				{
					return TargetPlatform.WinRT;
				}
			}

			return this.GetPlatformTargetThroughModuleLocation(module);
		}

		private TargetPlatform GetPlatformTargetThroughModuleLocation(ModuleDefinition module)
		{
			string moduleLocation = module.FullyQualifiedName ?? module.FilePath;

			if (moduleLocation != null)
			{
				if (moduleLocation.Contains(SystemInformation.NETCORE_DIRECTORY))
				{
					return TargetPlatform.NetCore;
				}
			}

			return TargetPlatform.None;
		}
	}
}
