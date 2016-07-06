using Mono.Cecil.Cil;
using System.Collections.Generic;
using Telerik.JustDecompiler.Ast;
using Telerik.JustDecompiler.Ast.Expressions;
using Telerik.JustDecompiler.Ast.Statements;
using Telerik.JustDecompiler.Decompiler;
using System;
using System.Linq;
using Telerik.JustDecompiler.Common;

namespace Telerik.JustDecompiler.Steps
{
    class CreateCompilerOptimizedSwitchByStringStatementsStep : BaseCodeTransformer, IDecompilationStep
    {
        private CompilerOptimizedSwitchByStringData switchByStringData;

        public BlockStatement Process(DecompilationContext context, BlockStatement body)
        {
            this.switchByStringData = context.MethodContext.SwitchByStringData;
            if (this.switchByStringData.SwitchBlocksStartInstructions.Count == 0)
            {
                return body;
            }

            return (BlockStatement)Visit(body);
        }

        public override ICodeNode VisitIfElseIfStatement(IfElseIfStatement node)
        {
            bool isSwitchByString = false;
            foreach (KeyValuePair<int, List<int>> pair in this.switchByStringData.SwitchBlocksToCasesMap)
            {
                foreach (int caseOffset in pair.Value)
                {
                    if (node.SearchableUnderlyingSameMethodInstructionOffsets.Contains(caseOffset))
                    {
                        isSwitchByString = true;
                        break;
                    }
                }
            }

            if (!isSwitchByString)
            {
                return base.VisitIfElseIfStatement(node);
            }

            bool isMatch = true;
            Expression firstSwitchExpression = null;
            List<int> switchExpressionLoadInstructions = new List<int>();
            foreach (KeyValuePair<Expression, BlockStatement> pair in node.ConditionBlocks)
            {
                if (pair.Key.CodeNodeType != CodeNodeType.UnaryExpression)
                {
                    isMatch = false;
                    break;
                }

                UnaryExpression unary = pair.Key as UnaryExpression;
                if (unary.Operator != UnaryOperator.None ||
                    unary.Operand.CodeNodeType != CodeNodeType.BinaryExpression)
                {
                    isMatch = false;
                    break;
                }

                BinaryExpression binary = unary.Operand as BinaryExpression;
                if (binary.Right.CodeNodeType != CodeNodeType.LiteralExpression ||
                    binary.Operator != BinaryOperator.ValueEquality)
                {
                    isMatch = false;
                    break;
                }

                LiteralExpression literal = binary.Right as LiteralExpression;
                if (literal.ExpressionType.FullName != "System.String")
                {
                    isMatch = false;
                    break;
                }

                if (firstSwitchExpression == null)
                {
                    firstSwitchExpression = binary.Left;
                }
                else if (!firstSwitchExpression.Equals(binary.Left))
                {
                    isMatch = false;
                    break;
                }
                else
                {
                    switchExpressionLoadInstructions.Add(binary.Left.UnderlyingSameMethodInstructions.First().Offset);
                }
            }

            if (!isMatch)
            {
                return base.VisitIfElseIfStatement(node);
            }

            CompilerOptimizedSwitchByStringStatement @switch = new CompilerOptimizedSwitchByStringStatement(firstSwitchExpression, switchExpressionLoadInstructions);
            foreach (KeyValuePair<Expression, BlockStatement> pair in node.ConditionBlocks)
            {
                if (SwitchHelpers.BlockHasFallThroughSemantics(pair.Value))
                {
                    pair.Value.AddStatement(new BreakSwitchCaseStatement());
                }

                Expression condition = ((pair.Key as UnaryExpression).Operand as BinaryExpression).Right;
                @switch.AddCase(new ConditionCase(condition, pair.Value));
            }

            if (node.Else != null)
            {
                if (SwitchHelpers.BlockHasFallThroughSemantics(node.Else))
                {
                    node.Else.AddStatement(new BreakSwitchCaseStatement());
                }

                @switch.AddCase(new DefaultCase(node.Else));
            }

            return Visit(@switch);
        }
    }
}
