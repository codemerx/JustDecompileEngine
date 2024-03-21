using Mono.Cecil;
using System;
using System.Runtime.CompilerServices;
using Telerik.JustDecompiler.Ast.Statements;

namespace Telerik.JustDecompiler.Decompiler
{
	internal class FilterMethodToBeDecompiled
	{
		public BlockStatement Block
		{
			get;
			private set;
		}

		public Telerik.JustDecompiler.Ast.Statements.CatchClause CatchClause
		{
			get;
			private set;
		}

		public DecompilationContext Context
		{
			get;
			private set;
		}

		public MethodDefinition Method
		{
			get;
			private set;
		}

		public FilterMethodToBeDecompiled(MethodDefinition method, Telerik.JustDecompiler.Ast.Statements.CatchClause catchClause, DecompilationContext context, BlockStatement block)
		{
			this.Method = method;
			this.CatchClause = catchClause;
			this.Context = context;
			this.Block = block;
		}
	}
}