using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Telerik.JustDecompiler.Ast;
using Telerik.JustDecompiler.Ast.Expressions;
using Telerik.JustDecompiler.Ast.Statements;
using Telerik.JustDecompiler.Decompiler;

namespace Telerik.JustDecompiler.Steps.CodePatterns
{
	class FieldInitializationPattern : CommonPatterns, ICodePattern
	{
		private readonly TypeSpecificContext typeContext;
		private readonly MethodDefinition method;

		public FieldInitializationPattern(CodePatternsContext patternsContext, DecompilationContext context)
			: base(patternsContext, context.MethodContext.Method.Module.TypeSystem)
		{
            this.typeContext = context.TypeContext;
			this.method = context.MethodContext.Method;
		}

		public bool TryMatch(StatementCollection statements,out int startIndex, out Statement result, out int replacedStatementsCount)
		{
			result = null;
			startIndex = 0;
			bool matched = TryMatchArrayAssignmentInternal(statements);
			if (matched)
			{
				replacedStatementsCount = 2;
				return true;
			}
			replacedStatementsCount = 1;
			return TryMatchDirectFieldAssignmentInternal(statements);
		}

		/// Pattern:
		/// variable = Expression;
		/// field = variable;
		/// where
		/// Expression is either array creation expression, literal constant, consturctor call from another class
		/// or combination of all of the above.
		private bool TryMatchArrayAssignmentInternal(StatementCollection statements)
		{
			if (statements.Count < 2)
			{
				return false;
			}
			ExpressionStatement theStatement = statements[0] as ExpressionStatement;
			if (theStatement == null)
			{
				return false;
			}
			BinaryExpression theAssignment = theStatement.Expression as BinaryExpression;
			VariableReference variable;
			if (theAssignment == null || !IsAssignToVariableExpression(theAssignment, out variable))
			{
				return false;
			}

			/// A check of wheather the assigned value can be used in field declaration context could be performed.
			/// At the moment, no IL samples that violate this rule were found.
			Expression assignedValue = theAssignment.Right;

			ExpressionStatement fieldAssignmentStatement = statements[1] as ExpressionStatement;
			if (fieldAssignmentStatement == null)
			{
				return false;
			}

			Expression variableReference;
			FieldReference theField;
			if (!IsFieldAssignment(fieldAssignmentStatement, out variableReference, out theField))
			{
				return false;
			}

			if (variableReference.CodeNodeType != CodeNodeType.VariableReferenceExpression ||
				(variableReference as VariableReferenceExpression).Variable != variable)
			{
				return false;
			}

			/// The simple name of the field can be used here as well.
			/// Using the full name for consistency with other maps.

			return MapFieldAssignmentIntoContext(theField, assignedValue);
		}

		/// Pattern:
		/// field = Expression;
		/// where
		/// Expression is either array creation expression, literal constant, consturctor call from another class
		/// or combination of all of the above.
		private bool TryMatchDirectFieldAssignmentInternal(StatementCollection statements)
		{
			ExpressionStatement theStatement = statements[0] as ExpressionStatement;
			if (theStatement == null || !string.IsNullOrEmpty(statements[0].Label))
			{
				return false;
			}

			FieldReference theField;
			Expression assignedValue;
			if (!IsFieldAssignment(theStatement, out assignedValue, out theField))
			{
				return false;
			}
			/// A check of wheather the assigned value can be used in field declaration context could be performed.
			/// At the moment, no IL samples that violate this rule were found.
			/// The simple name of the field can be used here as well.
			/// Using the full name for consistency with other maps.

			return MapFieldAssignmentIntoContext(theField, assignedValue);
		}
  
		private bool MapFieldAssignmentIntoContext(FieldReference theField, Expression assignedValue)
		{
			/// With the current workflow, each method is decompiled using new TypeContext.
			/// Thus, each constructor will be decompiled with new TypeContext, making
			/// the check for collisions pointless.
			/// All type contexts are merged in the ContextService, and all checks about the validity
			/// of the FieldAssignmentData dictionary are made there.

			typeContext.FieldAssignmentData.Add(theField.FullName, new FieldInitializationAssignment(this.method, assignedValue));
			return true;
		}

		private bool IsFieldAssignment(ExpressionStatement fieldAssignmentStatement, out Expression assignedValue, out FieldReference theField)
		{
			theField = null;
			assignedValue = null;
			BinaryExpression theFieldAssignment = fieldAssignmentStatement.Expression as BinaryExpression;
			if (theFieldAssignment == null || !theFieldAssignment.IsAssignmentExpression || theFieldAssignment.Left.CodeNodeType != CodeNodeType.FieldReferenceExpression)
			{
				return false;
			}

			theField = (theFieldAssignment.Left as FieldReferenceExpression).Field;
			if (theField == null)
			{
				return false;
			}

			assignedValue = theFieldAssignment.Right;

			return true;
		}
	}
}
