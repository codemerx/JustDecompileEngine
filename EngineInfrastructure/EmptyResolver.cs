using Telerik.JustDecompiler.External;
using Telerik.JustDecompiler.External.Interfaces;

namespace JustDecompile.EngineInfrastructure
{
    public class EmptyResolver : IFrameworkResolver
    {
        public FrameworkVersion GetDefaultFallbackFramework4Version()
        {
            return FrameworkVersion.Unknown;
        }
    }
}
