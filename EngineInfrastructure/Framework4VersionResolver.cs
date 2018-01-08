using SystemInformationHelpers;

namespace JustDecompile.EngineInfrastructure
{
    public static class Framework4VersionResolver
    {
        public static Telerik.JustDecompiler.External.FrameworkVersion GetInstalledFramework4Version()
        {
            FrameworkVersion framework4VersionToParse = SystemInformationHelpers.Framework4VersionResolver.GetInstalledFramework4Version();

            return MapFrameworkVersion(framework4VersionToParse);
        }

        public static Telerik.JustDecompiler.External.FrameworkVersion GetFrameworkVersionByFileVersion(string assemblyFilePath)
        {
            FrameworkVersion framework4VersionToParse = SystemInformationHelpers.Framework4VersionResolver.GetFrameworkVersionByFileVersion(assemblyFilePath);

            return MapFrameworkVersion(framework4VersionToParse);
        }

        private static Telerik.JustDecompiler.External.FrameworkVersion MapFrameworkVersion(FrameworkVersion frameworkVersionToParse)
        {
            Telerik.JustDecompiler.External.FrameworkVersion resultFrameworkVersion = Telerik.JustDecompiler.External.FrameworkVersion.Unknown;

            switch (frameworkVersionToParse)
            {
                case FrameworkVersion.v4_0:
                    resultFrameworkVersion = Telerik.JustDecompiler.External.FrameworkVersion.v4_0;
                    break;
                case FrameworkVersion.v4_5:
                    resultFrameworkVersion = Telerik.JustDecompiler.External.FrameworkVersion.v4_5;
                    break;
                case FrameworkVersion.v4_5_1:
                    resultFrameworkVersion = Telerik.JustDecompiler.External.FrameworkVersion.v4_5_1;
                    break;
                case FrameworkVersion.v4_5_2:
                    resultFrameworkVersion = Telerik.JustDecompiler.External.FrameworkVersion.v4_5_2;
                    break;
                case FrameworkVersion.v4_6:
                    resultFrameworkVersion = Telerik.JustDecompiler.External.FrameworkVersion.v4_6;
                    break;
                case FrameworkVersion.v4_6_1:
                    resultFrameworkVersion = Telerik.JustDecompiler.External.FrameworkVersion.v4_6_1;
                    break;
                case FrameworkVersion.v4_6_2:
                    resultFrameworkVersion = Telerik.JustDecompiler.External.FrameworkVersion.v4_6_2;
                    break;
				case FrameworkVersion.v4_7:
					resultFrameworkVersion = Telerik.JustDecompiler.External.FrameworkVersion.v4_7;
					break;
				case FrameworkVersion.v4_7_1:
					resultFrameworkVersion = Telerik.JustDecompiler.External.FrameworkVersion.v4_7_1;
					break;
				default:
                    break;
            }

            return resultFrameworkVersion;
        }
    }
}