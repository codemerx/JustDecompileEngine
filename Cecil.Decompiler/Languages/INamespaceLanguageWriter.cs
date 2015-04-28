using System.Collections.Generic;
using Mono.Cecil;
using Telerik.JustDecompiler.Decompiler.WriterContextServices;

namespace Telerik.JustDecompiler.Languages
{
	public interface INamespaceLanguageWriter : ILanguageWriter
	{
		List<WritingInfo> WritePartialTypeAndNamespaces(TypeDefinition type, IWriterContextService writerContextService, bool showCompilerGeneratedMembers, bool writeFullyQualifiedNames, bool writeDocumentation, Dictionary<string, ICollection<string>> fieldsToSkip = null);

		List<WritingInfo> WriteTypeAndNamespaces(TypeDefinition type, IWriterContextService writerContextService, bool writeDocumentation, bool showCompilerGeneratedMembers, bool writeFullyQualifiedNames);

		List<WritingInfo> WriteType(TypeDefinition type, IWriterContextService writerContextService, bool writeDocumentation, bool showCompilerGeneratedMembers, bool writeFullyQualifiedNames);

		void WriteBody(IMemberDefinition member, IWriterContextService writerContextService, bool writeFullyQualifiedNames);
	}
}
