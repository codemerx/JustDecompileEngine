using System;
using System.Collections.Generic;
using Telerik.JustDecompiler.Ast;
using Telerik.JustDecompiler.Ast.Expressions;
using Telerik.JustDecompiler.Ast.Statements;
using Telerik.JustDecompiler.Decompiler;
using Mono.Cecil;
using Mono.Cecil.Extensions;
using Mono.Cecil.Cil;

namespace Telerik.JustDecompiler.Steps
{
    class RemoveDelegateCaching : BaseCodeTransformer, IDecompilationStep
    {
        private DecompilationContext context;
        private Dictionary<FieldDefinition, Expression> fieldsToRemove;
        private Dictionary<VariableReference, Expression> variablesToRemove;
        private Dictionary<VariableReference, Statement> initializationsToRemove;

        public BlockStatement Process(DecompilationContext context, BlockStatement body)
        {
            this.context = context;
            this.fieldsToRemove = new Dictionary<FieldDefinition, Expression>();
            this.variablesToRemove = new Dictionary<VariableReference, Expression>();
            this.initializationsToRemove = new Dictionary<VariableReference, Statement>();
            BlockStatement result = (BlockStatement)Visit(body);
            RemoveInitializations();
            return result;
        }

        private void RemoveInitializations()
        {
            foreach (KeyValuePair<VariableReference, Statement> pair in initializationsToRemove)
            {
                if (!variablesToRemove.ContainsKey(pair.Key))
                {
                    continue;
                }

                BlockStatement parentBlock = pair.Value.Parent as BlockStatement;
                if (parentBlock == null)
                {
                    throw new Exception("Invalid parent statement.");
                }

                this.context.MethodContext.Variables.Remove(pair.Key.Resolve());
                parentBlock.Statements.Remove(pair.Value);
            }
        }

        public override ICodeNode VisitIfStatement(IfStatement node)
        {
            if (CheckIfStatement(node))
            {
                return null;
            }

            return base.VisitIfStatement(node);
        }

        private bool CheckIfStatement(IfStatement theIf)
        {
            if (theIf.Else != null || theIf.Then.Statements.Count != 1 || theIf.Then.Statements[0].CodeNodeType != CodeNodeType.ExpressionStatement ||
                theIf.Condition.CodeNodeType != CodeNodeType.BinaryExpression)
            {
                return false;
            }

            BinaryExpression theCondition = theIf.Condition as BinaryExpression;

            if (theCondition.Left.CodeNodeType == CodeNodeType.FieldReferenceExpression)
            {
                return CheckFieldCaching(theIf);
            }
            else if (theCondition.Left.CodeNodeType == CodeNodeType.VariableReferenceExpression)
            {
                return CheckVariableCaching(theIf);
            }

            return false;
        }

        private bool CheckFieldCaching(IfStatement theIf)
        {
            BinaryExpression theCondition = theIf.Condition as BinaryExpression;

            if (theCondition.Operator != BinaryOperator.ValueEquality || theCondition.Right.CodeNodeType != CodeNodeType.LiteralExpression ||
                (theCondition.Right as LiteralExpression).Value != null)
            {
                return false;
            }

            FieldDefinition theFieldDef = (theCondition.Left as FieldReferenceExpression).Field.Resolve();
            if (theFieldDef == null || !theFieldDef.IsStatic || !theFieldDef.IsPrivate)
            {
                return false;
            }

            BinaryExpression theAssignExpression = (theIf.Then.Statements[0] as ExpressionStatement).Expression as BinaryExpression;
            if (theAssignExpression == null || !theAssignExpression.IsAssignmentExpression ||
                theAssignExpression.Left.CodeNodeType != CodeNodeType.FieldReferenceExpression ||
                (theAssignExpression.Left as FieldReferenceExpression).Field.Resolve() != theFieldDef)
            {
                return false;
            }

            if (fieldsToRemove.ContainsKey(theFieldDef))
            {
                throw new Exception("A caching field cannot be assigned more than once.");
            }

            //Slow checks
            if (!theFieldDef.IsCompilerGenerated())
            {
                return false;
            }

            TypeDefinition fieldTypeDef = theFieldDef.FieldType.Resolve();
            if (fieldTypeDef == null || fieldTypeDef.BaseType == null || fieldTypeDef.BaseType.FullName != "System.MulticastDelegate")
            {
                return false;
            }

            fieldsToRemove[theFieldDef] = theAssignExpression.Right;
            return true;
        }

        private bool CheckVariableCaching(IfStatement theIf)
        {
            BinaryExpression theCondition = theIf.Condition as BinaryExpression;

            if (theCondition.Operator != BinaryOperator.ValueEquality || theCondition.Right.CodeNodeType != CodeNodeType.LiteralExpression ||
                (theCondition.Right as LiteralExpression).Value != null)
            {
                return false;
            }

            VariableReference theVariable = (theCondition.Left as VariableReferenceExpression).Variable;
            if (!initializationsToRemove.ContainsKey(theVariable))
            {
                return false;
            }

            BinaryExpression theAssignExpression = (theIf.Then.Statements[0] as ExpressionStatement).Expression as BinaryExpression;
            if (theAssignExpression == null || !theAssignExpression.IsAssignmentExpression ||
                theAssignExpression.Left.CodeNodeType != CodeNodeType.VariableReferenceExpression ||
                (theAssignExpression.Left as VariableReferenceExpression).Variable != theVariable)
            {
                return false;
            }

            if (variablesToRemove.ContainsKey(theVariable))
            {
                throw new Exception("A caching variable cannot be assigned more than once.");
            }

            //Slow checks
            TypeDefinition variableTypeDef = theVariable.VariableType.Resolve();
            if (variableTypeDef == null || variableTypeDef.BaseType == null || variableTypeDef.BaseType.FullName != "System.MulticastDelegate")
            {
                return false;
            }

            variablesToRemove[theVariable] = theAssignExpression.Right;
            return true;
        }

        public override ICodeNode VisitFieldReferenceExpression(FieldReferenceExpression node)
        {
            Expression fieldValue;
            FieldDefinition theFieldDef = node.Field.Resolve();
            if (theFieldDef != null && fieldsToRemove.TryGetValue(theFieldDef, out fieldValue))
            {
                return fieldValue.CloneExpressionOnlyAndAttachInstructions(node.UnderlyingSameMethodInstructions);
            }

            return base.VisitFieldReferenceExpression(node);
        }

        public override ICodeNode VisitVariableReferenceExpression(VariableReferenceExpression node)
        {
            Expression variableValue;
            if (variablesToRemove.TryGetValue(node.Variable, out variableValue))
            {
                return variableValue.CloneExpressionOnlyAndAttachInstructions(node.UnderlyingSameMethodInstructions);
            }

            return base.VisitVariableReferenceExpression(node);
        }

        public override ICodeNode VisitBinaryExpression(BinaryExpression node)
        {
            if (node.IsAssignmentExpression)
            {
                if (node.Left.CodeNodeType == CodeNodeType.FieldReferenceExpression)
                {
                    FieldDefinition theFieldDef = (node.Left as FieldReferenceExpression).Field.Resolve();
                    if (theFieldDef != null && fieldsToRemove.ContainsKey(theFieldDef))
                    {
                        throw new Exception("A caching field cannot be assigned more than once.");
                    }
                }
                else if (node.Left.CodeNodeType == CodeNodeType.VariableReferenceExpression)
                {
                    if (variablesToRemove.ContainsKey((node.Left as VariableReferenceExpression).Variable))
                    {
                        throw new Exception("A caching variable cannot be assigned more than once.");
                    }
                }
            }

            return base.VisitBinaryExpression(node);
        }

        public override ICodeNode VisitExpressionStatement(ExpressionStatement node)
        {
            if (CheckVariableInitialization(node))
            {
                return node;
            }
            return base.VisitExpressionStatement(node);
        }

        private bool CheckVariableInitialization(ExpressionStatement node)
        {
            if (!node.IsAssignmentStatement())
            {
                return false;
            }

            BinaryExpression theAssignExpression = node.Expression as BinaryExpression;

            if (theAssignExpression.Left.CodeNodeType != CodeNodeType.VariableReferenceExpression)
            {
                return false;
            }

            Expression value = theAssignExpression.Right;
            if (value.CodeNodeType == CodeNodeType.CastExpression)
            {
                value = (value as CastExpression).Expression;
            }

            if (value.CodeNodeType != CodeNodeType.LiteralExpression || (value as LiteralExpression).Value != null)
            {
                return false;
            }

            initializationsToRemove[(theAssignExpression.Left as VariableReferenceExpression).Variable] = node;
            return true;
        }
    }
}
