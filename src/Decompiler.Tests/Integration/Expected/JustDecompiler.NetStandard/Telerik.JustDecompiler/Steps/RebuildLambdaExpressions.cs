using Mono.Cecil;
using Mono.Cecil.Extensions;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Telerik.JustDecompiler.Ast;
using Telerik.JustDecompiler.Ast.Expressions;
using Telerik.JustDecompiler.Ast.Statements;
using Telerik.JustDecompiler.Common;
using Telerik.JustDecompiler.Decompiler;

namespace Telerik.JustDecompiler.Steps
{
	public class RebuildLambdaExpressions : BaseCodeTransformer, IDecompilationStep
	{
		private DecompilationContext context;

		public RebuildLambdaExpressions()
		{
		}

		public bool CheckTypeForCompilerGeneratedAttribute(TypeDefinition typeDefinition)
		{
			while (typeDefinition.get_IsNested())
			{
				if (typeDefinition.HasCompilerGeneratedAttribute())
				{
					return true;
				}
				typeDefinition = typeDefinition.get_DeclaringType();
			}
			return false;
		}

		private DecompilationContext CreateDecompilationContext(MethodDefinition lambdaMethodDefinition)
		{
			if ((object)lambdaMethodDefinition.get_DeclaringType() != (object)this.context.TypeContext.CurrentType)
			{
				return new DecompilationContext(new MethodSpecificContext(lambdaMethodDefinition.get_Body()), new TypeSpecificContext(lambdaMethodDefinition.get_DeclaringType()), this.context.Language);
			}
			return new DecompilationContext(new MethodSpecificContext(lambdaMethodDefinition.get_Body()), this.context.TypeContext, this.context.ModuleContext, this.context.AssemblyContext, this.context.Language);
		}

		public BlockStatement Process(DecompilationContext context, BlockStatement body)
		{
			this.context = context;
			return (BlockStatement)this.VisitBlockStatement(body);
		}

		public override ICodeNode VisitMethodInvocationExpression(MethodInvocationExpression node)
		{
			node.MethodExpression = (MethodReferenceExpression)base.VisitMethodReferenceExpression(node.MethodExpression);
			node.Arguments = (ExpressionCollection)this.Visit(node.Arguments);
			return node;
		}

		public override ICodeNode VisitMethodReferenceExpression(MethodReferenceExpression node)
		{
			MethodDefinition methodDefinition = node.Method.Resolve();
			TypeDefinition currentType = this.context.TypeContext.CurrentType;
			if (methodDefinition == null || (object)methodDefinition.get_DeclaringType() != (object)currentType && !methodDefinition.get_DeclaringType().IsNestedIn(currentType))
			{
				return base.VisitMethodReferenceExpression(node);
			}
			if (methodDefinition.get_IsGetter() || methodDefinition.get_IsSetter() || !methodDefinition.IsCompilerGenerated(true) && !this.CheckTypeForCompilerGeneratedAttribute(methodDefinition.get_DeclaringType()))
			{
				return base.VisitMethodReferenceExpression(node);
			}
			BlockStatement blockStatement = null;
			if (methodDefinition.get_Body() != null)
			{
				DecompilationContext decompilationContext = this.CreateDecompilationContext(methodDefinition);
				blockStatement = methodDefinition.get_Body().DecompileLambda(this.context.Language, decompilationContext);
				if (blockStatement.Statements.Count == 1 && blockStatement.Statements[0].CodeNodeType == CodeNodeType.ExpressionStatement && (blockStatement.Statements[0] as ExpressionStatement).Expression.CodeNodeType == CodeNodeType.ReturnExpression)
				{
					ReturnExpression expression = (blockStatement.Statements[0] as ExpressionStatement).Expression as ReturnExpression;
					ShortFormReturnExpression shortFormReturnExpression = new ShortFormReturnExpression(expression.Value, expression.MappedInstructions);
					blockStatement = new BlockStatement();
					blockStatement.Statements.Add(new ExpressionStatement(shortFormReturnExpression));
				}
				this.context.MethodContext.VariableDefinitionToNameMap.AddRange<VariableDefinition, string>(decompilationContext.MethodContext.VariableDefinitionToNameMap);
				this.context.MethodContext.VariableNamesCollection.UnionWith(decompilationContext.MethodContext.VariableNamesCollection);
				this.context.MethodContext.AddInnerMethodParametersToContext(decompilationContext.MethodContext);
				this.context.MethodContext.GotoStatements.AddRange(decompilationContext.MethodContext.GotoStatements);
				this.context.MethodContext.GotoLabels.AddRange<string, Statement>(decompilationContext.MethodContext.GotoLabels);
			}
			ExpressionCollection expressionCollection = new ExpressionCollection();
			bool flag = LambdaExpressionsHelper.HasAnonymousParameter(methodDefinition.get_Parameters());
			foreach (ParameterDefinition parameter in methodDefinition.get_Parameters())
			{
				expressionCollection.Add(new LambdaParameterExpression(parameter, !flag, null));
			}
			return new LambdaExpression(expressionCollection, blockStatement, methodDefinition.IsAsync(), methodDefinition.IsFunction(), node.Method.get_Parameters(), false, node.MappedInstructions);
		}

		public override ICodeNode VisitObjectCreationExpression(ObjectCreationExpression node)
		{
			base.VisitObjectCreationExpression(node);
			if (node.Constructor == null)
			{
				return node;
			}
			TypeDefinition typeDefinition = node.Constructor.get_DeclaringType().Resolve();
			if (typeDefinition == null || typeDefinition.get_BaseType() == null || typeDefinition.get_BaseType().get_FullName() != "System.MulticastDelegate" || node.Arguments.Count != 2 || node.Arguments[1].CodeNodeType != CodeNodeType.LambdaExpression)
			{
				return node;
			}
			(node.Arguments[1] as LambdaExpression).ExpressionType = typeDefinition;
			Expression item = node.Arguments[0];
			return new DelegateCreationExpression(node.ExpressionType, node.Arguments[1], item, node.MappedInstructions);
		}
	}
}