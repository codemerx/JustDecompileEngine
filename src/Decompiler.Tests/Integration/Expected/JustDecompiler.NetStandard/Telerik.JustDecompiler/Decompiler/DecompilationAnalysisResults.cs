using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Telerik.JustDecompiler.Ast.Expressions;

namespace Telerik.JustDecompiler.Decompiler
{
	public class DecompilationAnalysisResults
	{
		public HashSet<ExplicitCastExpression> AmbiguousCastsToObject
		{
			get;
			private set;
		}

		public HashSet<TypeReference> TypesDependingOn
		{
			get;
			private set;
		}

		public DecompilationAnalysisResults()
		{
			this.TypesDependingOn = new HashSet<TypeReference>();
			this.AmbiguousCastsToObject = new HashSet<ExplicitCastExpression>();
		}
	}
}