using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.JustDecompiler.Ast.Expressions;
using Mono.Cecil;

namespace Telerik.JustDecompiler.Decompiler
{
	public class FieldInitializationAssignment
	{
		public MethodDefinition AssignmentMethod { get; private set; }
		public Expression AssignmentExpression { get; private set; }

		public FieldInitializationAssignment(MethodDefinition assignmentMethod, Expression assignmentExpression)
		{
			this.AssignmentMethod = assignmentMethod;
			this.AssignmentExpression = assignmentExpression;
		}
	}
}
