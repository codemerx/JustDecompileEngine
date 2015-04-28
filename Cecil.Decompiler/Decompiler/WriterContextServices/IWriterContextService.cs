using System;
using System.Linq;
using Mono.Cecil;
using Telerik.JustDecompiler.Languages;
using System.Collections.Generic;

namespace Telerik.JustDecompiler.Decompiler.WriterContextServices
{
	public interface IWriterContextService
	{
		WriterContext GetWriterContext(IMemberDefinition member, ILanguage language);
		ModuleSpecificContext GetModuleContext(ModuleDefinition module, ILanguage language);
		AssemblySpecificContext GetAssemblyContext(AssemblyDefinition assembly, ILanguage language);

		ICollection<MethodDefinition> ExceptionsWhileDecompiling { get; }
	}
}
