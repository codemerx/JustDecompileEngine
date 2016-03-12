using System;
using System.Linq;
using Telerik.JustDecompiler.Decompiler;

namespace Telerik.JustDecompiler.Languages.CSharp
{
	public class CSharpAssemblyAttributeWriter : BaseAssemblyAttributeWriter
	{
		private readonly CSharpAssemblyAttributeInternalWriter writer;

		public CSharpAssemblyAttributeWriter(ILanguage language, IFormatter formatter, IExceptionFormatter exceptionFormatter, bool writeExceptionsAsComments)
			: base(language, exceptionFormatter, writeExceptionsAsComments)
        {
			this.writer = new CSharpAssemblyAttributeInternalWriter(language, formatter, exceptionFormatter, writeExceptionsAsComments);
            this.writer.ExceptionThrown += OnExceptionThrown;
        }

		protected override void SetAssemblyContext(AssemblySpecificContext assemblyContext)
		{
			writer.InternalAssemblyContext = assemblyContext;
		}

		protected override void SetModuleContext(ModuleSpecificContext moduleContext)
		{
			writer.InternalModuleContext = moduleContext;
		}

		protected override AttributeWriter CreateAttributeWriter()
		{
			return new CSharpAttributeWriter(this.writer);
		}

		protected override NamespaceImperativeLanguageWriter GetLanguageWriter()
		{
			return this.writer;
		}

		private class CSharpAssemblyAttributeInternalWriter : CSharpWriter
		{
			public AssemblySpecificContext InternalAssemblyContext { get; set; }

			public ModuleSpecificContext InternalModuleContext { get; set; }

			public CSharpAssemblyAttributeInternalWriter(ILanguage language, IFormatter formatter, IExceptionFormatter exceptionFormatter, bool writeExceptionsAsComments)
				: base(language, formatter, exceptionFormatter, writeExceptionsAsComments)
			{
			}

			protected override AssemblySpecificContext AssemblyContext
			{
				get
				{
					return this.InternalAssemblyContext;
				}
			}

			protected override ModuleSpecificContext ModuleContext
			{
				get
				{
					return this.InternalModuleContext;
				}
			}

			protected override TypeSpecificContext TypeContext
			{
				get
				{
					return null;
				}
			}

			protected override bool IsTypeNameInCollision(string typeName)
			{
				return Utilities.IsTypeNameInCollisionOnAssemblyLevel(typeName, AssemblyContext, ModuleContext);
			}
		}
	}
}
