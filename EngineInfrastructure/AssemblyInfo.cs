using Mono.Cecil;
using System;
using System.Collections.Generic;
using Telerik.JustDecompiler.External;
using Telerik.JustDecompiler.Common;

namespace JustDecompile.EngineInfrastructure
{
    public class AssemblyInfo
    {
        public AssemblyInfo()
        {
            this.ModulesFrameworkVersions = new Dictionary<ModuleDefinition, FrameworkVersion>();
        }

        public IDictionary<ModuleDefinition, FrameworkVersion> ModulesFrameworkVersions { get; private set; }
    }
}