namespace Telerik.JustDecompiler.Decompiler
{
	public class DecompilationContext
	{
		public MethodSpecificContext MethodContext { get; set; }

		public TypeSpecificContext TypeContext { get; set; }

		public ModuleSpecificContext ModuleContext { get; set; }

		public AssemblySpecificContext AssemblyContext { get; set; }

		public DecompilationContext(MethodSpecificContext methodContext, TypeSpecificContext typeContext)
			: this(methodContext, typeContext, new ModuleSpecificContext(), new AssemblySpecificContext())
		{
		}

		public DecompilationContext(MethodSpecificContext methodContext, TypeSpecificContext typeContext, ModuleSpecificContext moduleContext, AssemblySpecificContext assemblyContext)
		{
			this.MethodContext = methodContext;
			this.TypeContext = typeContext;
			this.ModuleContext = moduleContext;
			this.AssemblyContext = assemblyContext;
		}

		public DecompilationContext() { }
	}
}