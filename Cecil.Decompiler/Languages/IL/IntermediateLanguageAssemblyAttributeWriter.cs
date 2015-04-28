using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Telerik.JustDecompiler.Decompiler.WriterContextServices;

namespace Telerik.JustDecompiler.Languages.IL
{
	public class IntermediateLanguageAssemblyAttributeWriter : IAssemblyAttributeWriter
	{
		protected IFormatter formatter;
		protected bool WriteExceptionsAsComments { get; private set; }
		protected IExceptionFormatter exceptionFormatter;
		private readonly bool shouldGenerateBlocks;

		public ILanguage Language { get; private set; }

		public IntermediateLanguageAssemblyAttributeWriter(ILanguage language, IFormatter formatter, IExceptionFormatter exceptionFormatter, bool writeExceptionsAsComments, bool shouldGenerateBlocks)
        {
            this.Language = language;
            this.formatter = formatter;
			this.exceptionFormatter = exceptionFormatter;
			this.WriteExceptionsAsComments = writeExceptionsAsComments;
			this.shouldGenerateBlocks = shouldGenerateBlocks;
		}

        public void WriteAssemblyAttributes(AssemblyDefinition assembly, IWriterContextService writerContextService, bool writeUsings = false, ICollection<string> attributesToIgnore = null)
        {
			IntermediateLanguageAttributeWriter attributeWriter = new IntermediateLanguageAttributeWriter(this.Language, this.formatter, this.exceptionFormatter, this.WriteExceptionsAsComments, this.shouldGenerateBlocks);
			attributeWriter.WriteAssemblyAttributes(assembly, attributesToIgnore);
		}

		public void WriteModuleAttributes(ModuleDefinition module, IWriterContextService writerContextService, bool writeUsings = false, ICollection<string> attributesToIgnore = null)
		{
		}

		public void WriteAssemblyInfo(AssemblyDefinition assembly, IWriterContextService writerContextService, bool writeUsings = false, 
			ICollection<string> assemblyAttributesToIgnore = null, ICollection<string> moduleAttributesToIgnore = null)
		{
			WriteAssemblyAttributes(assembly, writerContextService, writeUsings, assemblyAttributesToIgnore);
			WriteModuleAttributes(assembly.MainModule, writerContextService, writeUsings, moduleAttributesToIgnore);
		}
	}
}
