using System;
using System.Collections.Generic;
using Telerik.JustDecompiler.Ast;
using Telerik.JustDecompiler.Ast.Expressions;
using Telerik.JustDecompiler.Ast.Statements;
using Telerik.JustDecompiler.Decompiler;
using Mono.Cecil.Cil;
using Mono.Cecil;

namespace Telerik.JustDecompiler.Steps
{
    class ManagedPointersRemovalStep : BaseCodeVisitor, IDecompilationStep
    {
        private DecompilationContext context;
        private readonly Dictionary<VariableDefinition, BinaryExpression> variableToAssignExpression =
            new Dictionary<VariableDefinition, BinaryExpression>();

        public BlockStatement Process(DecompilationContext context, BlockStatement body)
        {
            this.context = context;

            VisitExpressions();

            TransformExpressions(new VariableReplacer(variableToAssignExpression));
            TransformExpressions(new Dereferencer());

            RemoveVariablesFromContext();

            return body;
        }

        public void VisitExpressions()
        {
            foreach (IList<Expression> expressionList in this.context.MethodContext.Expressions.BlockExpressions.Values)
            {
                foreach (Expression expression in expressionList)
                {
                    Visit(expression);
                }
            }
        }

        public void TransformExpressions(BaseCodeTransformer transformer)
        {
            foreach (IList<Expression> expressionList in this.context.MethodContext.Expressions.BlockExpressions.Values)
            {
                int current_index = 0;
                for (int iterator_index = 0; iterator_index < expressionList.Count; iterator_index++)
                {
                    Expression result = (Expression)transformer.Visit(expressionList[iterator_index]);
                    if(result != null)
                    {
                        expressionList[current_index++] = result;
                    }
                }

                for (int i = expressionList.Count - current_index; i > 0; i--)
                {
                    expressionList.RemoveAt(current_index + i - 1);
                }
            }
        }

        private void RemoveVariablesFromContext()
        {
            foreach (VariableDefinition variable in variableToAssignExpression.Keys)
            {
                this.context.MethodContext.RemoveVariable(variable);
                this.context.MethodContext.VariablesToRename.Remove(variable);
                this.context.MethodContext.VariableAssignmentData.Remove(variable);
            }
        }

        public override void VisitBinaryExpression(BinaryExpression node)
        {
            if (!node.IsAssignmentExpression || !CheckForAssignment(node))
            {
                base.VisitBinaryExpression(node);
            }
        }

        private bool CheckForAssignment(BinaryExpression node)
        {
            if (node.Left.CodeNodeType == CodeNodeType.ArgumentReferenceExpression &&
                (node.Left as ArgumentReferenceExpression).Parameter.ParameterType.IsByReference)
            {
                throw new Exception("Managed pointer usage not in SSA");
            }

            if (node.Left.CodeNodeType != CodeNodeType.VariableReferenceExpression ||
                !(node.Left as VariableReferenceExpression).Variable.VariableType.IsByReference)
            {
                return false;
            }

            VariableDefinition byRefVariable = (node.Left as VariableReferenceExpression).Variable.Resolve();

            if (variableToAssignExpression.ContainsKey(byRefVariable))
            {
                throw new Exception("Managed pointer usage not in SSA");
            }

            variableToAssignExpression.Add(byRefVariable, node);
            return true;
        }

        public override void VisitUnaryExpression(UnaryExpression node)
        {
            if (node.Operator != UnaryOperator.AddressDereference || node.Operand.CodeNodeType != CodeNodeType.VariableReferenceExpression ||
                !variableToAssignExpression.ContainsKey((node.Operand as VariableReferenceExpression).Variable.Resolve()))
            {
                base.VisitUnaryExpression(node);
            }
        }

        private class VariableReplacer : BaseCodeTransformer
        {
            private readonly Dictionary<VariableDefinition, BinaryExpression> variableToAssignExpression;
            private readonly HashSet<BinaryExpression> expressionsToSkip = new HashSet<BinaryExpression>();

            public VariableReplacer(Dictionary<VariableDefinition, BinaryExpression> variableToAssignExpression)
            {
                this.variableToAssignExpression = variableToAssignExpression;
                this.expressionsToSkip = new HashSet<BinaryExpression>(variableToAssignExpression.Values);
            }

            public override ICodeNode VisitBinaryExpression(BinaryExpression node)
            {
                if (expressionsToSkip.Contains(node))
                {
                    return null;
                }
                return base.VisitBinaryExpression(node);
            }

            public override ICodeNode VisitVariableReferenceExpression(VariableReferenceExpression node)
            {
                Expression variableValue;
                return TryGetVariableValue(node.Variable.Resolve(), out variableValue) ? variableValue : node;
            }

            private bool TryGetVariableValue(VariableDefinition variable, out Expression value)
            {
                BinaryExpression assignExpression;
                if (!variableToAssignExpression.TryGetValue(variable, out assignExpression))
                {
                    value = null;
                    return false;
                }
                
                value = assignExpression.Right.CloneExpressionOnly();
                return true;
            }
        }

        private class Dereferencer : BaseCodeTransformer
        {
            public override ICodeNode VisitUnaryExpression(UnaryExpression node)
            {
                if (node.Operator == UnaryOperator.AddressDereference)
                {
                    if (node.Operand.CodeNodeType == CodeNodeType.ThisReferenceExpression)
                    {
                        return node.Operand;
                    }

                    UnaryExpression unaryOperand = node.Operand as UnaryExpression;
                    if (unaryOperand != null && (unaryOperand.Operator == UnaryOperator.AddressReference || unaryOperand.Operator == UnaryOperator.AddressOf))
                    {
                        return Visit(unaryOperand.Operand);
                    }

                    CastExpression castOperand = node.Operand as CastExpression;
                    if (castOperand != null && castOperand.TargetType.IsByReference)
                    {
                        TypeReference targetType = (castOperand.TargetType as ByReferenceType).ElementType;
                        return new CastExpression((Expression)Visit(castOperand.Expression), targetType, null);
                    }
                }
                return base.VisitUnaryExpression(node);
            }

            private ICodeNode VisitTargetExpression(Expression target)
            {
                if (target != null && target.CodeNodeType == CodeNodeType.UnaryExpression)
                {
                    UnaryExpression addressReferenceExpression = target as UnaryExpression;
                    if (addressReferenceExpression.Operator == UnaryOperator.AddressReference)
                    {
                        if (addressReferenceExpression.Operand.ExpressionType == null)
                        {
                            throw new Exception("Referenced element has no type.");
                        }

                        TypeReference typeReference = addressReferenceExpression.Operand.ExpressionType.Resolve() ??
                            addressReferenceExpression.Operand.ExpressionType;
                        if (typeReference.IsValueType)
                        {
                            return Visit(addressReferenceExpression.Operand);
                        }
                    }
                }
                return Visit(target);
            }

            public override ICodeNode VisitMethodReferenceExpression(MethodReferenceExpression node)
            {
                node.Target = (Expression)VisitTargetExpression(node.Target);
                return node;
            }

            public override ICodeNode VisitArrayIndexerExpression(ArrayIndexerExpression node)
            {
                node.Target = (Expression)VisitTargetExpression(node.Target);
                node.Indices = (ExpressionCollection)Visit(node.Indices);
                return node;
            }

            public override ICodeNode VisitArrayLengthExpression(ArrayLengthExpression node)
            {
                node.Target = (Expression)VisitTargetExpression(node.Target);
                return node;
            }

            public override ICodeNode VisitFieldReferenceExpression(FieldReferenceExpression node)
            {
                node.Target = (Expression)VisitTargetExpression(node.Target);
                return node;
            }

            public override ICodeNode VisitPropertyReferenceExpression(PropertyReferenceExpression node)
            {
                node.Target = (Expression)VisitTargetExpression(node.Target);
                node.Arguments = (ExpressionCollection)Visit(node.Arguments);
                return node;
            }

            public override ICodeNode VisitEventReferenceExpression(EventReferenceExpression node)
            {
                node.Target = (Expression)VisitTargetExpression(node.Target);
                return node;
            }
        }
    }
}
