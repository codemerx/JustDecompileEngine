using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.JustDecompiler.Ast;
using Telerik.JustDecompiler.Decompiler;
using Telerik.JustDecompiler.Ast.Statements;
using Telerik.JustDecompiler.Ast.Expressions;

namespace Telerik.JustDecompiler.Steps
{
	class DetermineNotSupportedVBCodeStep : BaseCodeTransformer, IDecompilationStep
	{
		public BlockStatement Process(DecompilationContext context, BlockStatement body)
		{
			NotSupportedUnaryOperatorFinder notSupportedUnaryOperatorFinder = new NotSupportedUnaryOperatorFinder();

			notSupportedUnaryOperatorFinder.Visit(body);

			if (notSupportedUnaryOperatorFinder.IsAddressUnaryOperatorFound)
			{
				throw new ArgumentException(string.Format("The unary opperator {0} is not supported in VisualBasic", notSupportedUnaryOperatorFinder.FoundUnaryOperator));
			}

			return body;
		}

		private class NotSupportedUnaryOperatorFinder : BaseCodeVisitor
		{
			public bool IsAddressUnaryOperatorFound;
			public UnaryOperator FoundUnaryOperator;
			private int methodInvocationsStackCount;

			public NotSupportedUnaryOperatorFinder()
			{
				this.IsAddressUnaryOperatorFound = false;
				this.FoundUnaryOperator = UnaryOperator.None;
				this.methodInvocationsStackCount = 0;
			}

			public override void Visit(ICodeNode node)
			{
				if (this.IsAddressUnaryOperatorFound)
				{
					return;
				}

				base.Visit(node);
			}

			public override void VisitMethodInvocationExpression(MethodInvocationExpression node)
			{
				Visit(node.MethodExpression);

				if (node.MethodExpression is MethodReferenceExpression)
				{
					this.methodInvocationsStackCount++;
				}

				Visit(node.Arguments);

				if (node.MethodExpression is MethodReferenceExpression)
				{
					this.methodInvocationsStackCount--;
				}
			}

			public override void  VisitUnaryExpression(UnaryExpression node)
			{
				if ((this.methodInvocationsStackCount == 0 && !(node.Operand is ArgumentReferenceExpression)) && 
					(node.Operator == UnaryOperator.AddressDereference || node.Operator == UnaryOperator.AddressReference || node.Operator == UnaryOperator.AddressOf))
				{
					this.IsAddressUnaryOperatorFound = true;
					this.FoundUnaryOperator = node.Operator;
				}

				base.VisitUnaryExpression(node);
			}
		}
	}
}
