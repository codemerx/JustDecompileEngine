using Mono.Cecil.Cil;
using System;
using Telerik.JustDecompiler.Ast.Statements;
using Telerik.JustDecompiler.Cil;
using Telerik.JustDecompiler.Decompiler;
using System.Collections.Generic;
using Telerik.JustDecompiler.Ast.Expressions;
using Telerik.JustDecompiler.Ast;
using System.Linq;

namespace Telerik.JustDecompiler.Steps
{
    /// <summary>
    /// This step is responsible for removing compiler optimizations.
    /// 
    /// Switch by string optimizations:
    /// The C# 6.0 compiler (aka Roslyn) emit some compiler optimizations which we try to remove in order to reproduce
    /// the switch as it was compiled. The algorithm works as follows:
    /// - We use the collection created by the ExpressionDecompilerStep called SwitchBlocksStartInstructions. It contains
    ///   the start instructions of all blocks that contain method call to the ComputeHashString.
    /// - If we suceed to extract the optimization variable and the switch expression from a block we traverse its children.
    ///   By doing that we are marking all the blocks that are generated as part of the optimization (called from now on
    ///   "optimization blocks") and all blocks that contain direct string comparison of the switch variable and the string
    ///   constant (called from now on case blocks).
    /// - After that we remove the last 2 expressions from the switch block found by the ExpressionDecompilerStep, since
    ///   they are the assignment of the optimization variable and the first check of it.
    /// - Then we merge the switch block found by the ExpressionDecompilerStep and the first case block, since there is
    ///   only one way of getting there.
    /// - Then we reconnect all case block in such way that the first case block has the second case block as second successor,
    ///   the second block has the third case block as second succesor, etc. This kind of structure will be transformed to nested
    ///   if-else statement and later on in if-elseif statement. The we use that if-elseif statements to create switch statements.
    /// - After all switches are fixed we remove all CFG blocks that are part of the optimizations.
    /// </summary>
    internal class RemoveCompilerOptimizationsStep : IDecompilationStep
    {
        private const string ComputeStringHashFullName = "System.UInt32 <PrivateImplementationDetails>::ComputeStringHash(System.String)";

        private MethodSpecificContext methodContext;
        private Dictionary<int, IList<Expression>> blockExpressions;
        private Dictionary<int, InstructionBlock> instructionToBlockMapping;

        /// <summary>
        /// Contains the start instructions of all blocks that have to be removed.
        /// </summary>
        private List<int> blocksToBeRemoved;
        private Dictionary<int, List<int>> switchBlocksToCasesMap;

        public BlockStatement Process(DecompilationContext context, BlockStatement body)
        {
            this.methodContext = context.MethodContext;
            this.blockExpressions = this.methodContext.Expressions.BlockExpressions;
            this.instructionToBlockMapping = this.methodContext.ControlFlowGraph.InstructionToBlockMapping;

            this.blocksToBeRemoved = new List<int>();
            this.switchBlocksToCasesMap = this.methodContext.SwitchByStringData.SwitchBlocksToCasesMap;

            RemoveSwitchByStringOptimizations();

            return body;
        }

        private void RemoveSwitchByStringOptimizations()
        {
            foreach (int index in this.methodContext.SwitchByStringData.SwitchBlocksStartInstructions)
            {
                SwitchData data;
                if (TryGetSwitchData(this.blockExpressions[index], out data))
                {
                    this.switchBlocksToCasesMap.Add(index, new List<int>());
                    MarkOptimizationAndCaseBlocks(this.instructionToBlockMapping[index], data);
                    // We need them sorted in order to preserve the original order of the cases.
                    this.switchBlocksToCasesMap[index].Sort();

                    RemoveExpressionsFromFirstBlock(index);

                    MergeFirstCFGBlockWithFirstCaseIf(index);

                    ReconnectBlocks(index);
                }
            }
            
            if (RemoveOptimizationBlocks())
            {
                this.methodContext.IsMethodBodyChanged = true;
            }
        }

        private bool TryGetSwitchData(IList<Expression> expressions, out SwitchData data)
        {
            data = null;

            if (expressions.Count < 2 ||
                expressions[expressions.Count - 2].CodeNodeType != CodeNodeType.BinaryExpression)
            {
                return false;
            }

            BinaryExpression binary = expressions[expressions.Count - 2] as BinaryExpression;
            if (!binary.IsAssignmentExpression)
            {
                return false;
            }

            if (binary.Left.CodeNodeType != CodeNodeType.VariableReferenceExpression ||
                binary.Right.CodeNodeType != CodeNodeType.MethodInvocationExpression)
            {
                return false;
            }

            MethodInvocationExpression methodInvocation = binary.Right as MethodInvocationExpression;
            if (methodInvocation.MethodExpression.Method.FullName != ComputeStringHashFullName)
            {
                return false;
            }

            data = new SwitchData((binary.Left as VariableReferenceExpression).Variable, methodInvocation.Arguments[0]);
            return true;
        }

        private void MarkOptimizationAndCaseBlocks(InstructionBlock block, SwitchData data)
        {
            Queue<InstructionBlock> queue = new Queue<InstructionBlock>();
            HashSet<int> visited = new HashSet<int>();

            foreach (InstructionBlock successor in block.Successors)
            {
                queue.Enqueue(successor);
            }

            while (queue.Count > 0)
            {
                InstructionBlock current = queue.Dequeue();
                visited.Add(current.First.Offset);

                if (IsOptimizationBlock(this.blockExpressions[current.First.Offset], data.OptimizationVariable))
                {
                    // The first successor of an optimization block is either another optimization block
                    // or case block. Either case we want to enqueue it.
                    InstructionBlock firstSuccessor = current.Successors[0];
                    if (!visited.Contains(firstSuccessor.First.Offset))
                    {
                        queue.Enqueue(firstSuccessor);
                    }

                    // The second (and last) successor of an optimization block can be unconditional jump.
                    // In that case we need to first remove it. Then we examine the new last successor of
                    // the optimization block.
                    InstructionBlock lastSuccessor = MarkSecondSuccessorForRemovalIfItIsUnconditionalJump(current);

                    // We enqueue the last successor of an optimization block only if it's another optimization block.
                    // This is done, because if we have an optimization block, which last successor is jump to the
                    // switch's next statement (the first statement after the actual switch) and this next statement
                    // have the same structure as the case blocks, this will cause unexpected behavior and wrong decompilation.
                    if (!visited.Contains(lastSuccessor.First.Offset) &&
                        IsOptimizationBlock(this.blockExpressions[current.First.Offset], data.OptimizationVariable))
                    {
                        queue.Enqueue(lastSuccessor);
                    }

                    blocksToBeRemoved.Add(current.First.Offset);
                }
                else if (IsCaseBlock(this.blockExpressions[current.First.Offset], data.SwitchExpression))
                {
                    switchBlocksToCasesMap[block.First.Offset].Add(current.First.Offset);

                    // If the second successor of the case block is unconditional jump we want to remove it.
                    // This is done, because later we take all case blocks and connect them one to each other.
                    // All case blocks can have different unconditional jump block used to jump the the next statement
                    // (the statement after the switch statement). Later we connect all the case blocks in a chain
                    // and we want to be sure that there is only one way to go the next statement, and that there are
                    // no unreachable unconditional jump blocks.
                    MarkSecondSuccessorForRemovalIfItIsUnconditionalJump(current);
                }
            }
        }

        private InstructionBlock MarkSecondSuccessorForRemovalIfItIsUnconditionalJump(InstructionBlock block)
        {
            InstructionBlock secondSuccessor = block.Successors[1];
            if (IsUnconditionJumpBlock(secondSuccessor))
            {
                block.RemoveFromSuccessors(secondSuccessor);
                block.AddToSuccessors(secondSuccessor.Successors[0]);
                this.blocksToBeRemoved.Add(secondSuccessor.First.Offset);

                secondSuccessor = secondSuccessor.Successors[0];
            }

            return secondSuccessor;
        }

        private bool IsUnconditionJumpBlock(InstructionBlock block)
        {
            return block.First == block.Last &&
                   (block.First.OpCode.Code == Code.Br || block.First.OpCode.Code == Code.Br_S) &&
                   block.Successors.Length == 1;
        }

        private bool IsOptimizationBlock(IList<Expression> expressions, VariableReference optimizationVariable)
        {
            if (expressions.Count != 1 ||
                expressions[0].CodeNodeType != CodeNodeType.BinaryExpression)
            {
                return false;
            }

            BinaryExpression binary = expressions[0] as BinaryExpression;
            if (binary.Left.CodeNodeType != CodeNodeType.VariableReferenceExpression ||
                binary.Right.CodeNodeType != CodeNodeType.LiteralExpression ||
                !binary.IsComparisonExpression ||
                (binary.Left as VariableReferenceExpression).Variable != optimizationVariable)
            {
                return false;
            }

            return true;
        }

        private bool IsCaseBlock(IList<Expression> expressions, Expression switchExpression)
        {
            if (expressions.Count != 1)
            {
                return false;
            }

            if (expressions[0].CodeNodeType == CodeNodeType.BinaryExpression)
            {
                return IsNullCaseBlock(expressions[0], switchExpression);
            }
            else if (expressions[0].CodeNodeType != CodeNodeType.UnaryExpression)
            {
                return false;
            }

            UnaryExpression unary = expressions[0] as UnaryExpression;
            if (unary.Operator != UnaryOperator.None ||
                unary.Operand.CodeNodeType != CodeNodeType.MethodInvocationExpression)
            {
                return false;
            }

            MethodInvocationExpression invocation = unary.Operand as MethodInvocationExpression;
            if (invocation.MethodExpression.Method.Name != "op_Equality" ||
                invocation.Arguments[1].CodeNodeType != CodeNodeType.LiteralExpression ||
                (invocation.Arguments[1] as LiteralExpression).ExpressionType.FullName != "System.String")
            {
                return false;
            }

            if (!invocation.Arguments[0].Equals(switchExpression))
            {
                return false;
            }

            return true;
        }

        private bool IsNullCaseBlock(Expression expression, Expression switchExpression)
        {
            if (expression.CodeNodeType != CodeNodeType.BinaryExpression)
            {
                return false;
            }

            BinaryExpression binary = expression as BinaryExpression;
            if (binary.Operator != BinaryOperator.ValueEquality ||
                binary.Right.CodeNodeType != CodeNodeType.LiteralExpression)
            {
                return false;
            }

            LiteralExpression literal = binary.Right as LiteralExpression;
            if (literal.ExpressionType.FullName != "System.Object" ||
                literal.Value != null)
            {
                return false;
            }

            if (!binary.Left.Equals(switchExpression))
            {
                return false;
            }

            return true;
        }

        private void RemoveExpressionsFromFirstBlock(int index)
        {
            this.blockExpressions[index].RemoveAt(this.blockExpressions[index].Count - 1);
            this.blockExpressions[index].RemoveAt(this.blockExpressions[index].Count - 1);
        }

        private void MergeFirstCFGBlockWithFirstCaseIf(int index)
        {
            InstructionBlock firstIf = this.instructionToBlockMapping[this.switchBlocksToCasesMap[index][0]];
            InstructionBlock firstBlock = this.instructionToBlockMapping[index];
            firstBlock.Last = firstIf.Last;
            firstBlock.Successors = firstIf.Successors;
            foreach (Expression expression in this.blockExpressions[firstIf.First.Offset])
            {
                this.blockExpressions[index].Add(expression);
            }

            this.blocksToBeRemoved.Add(firstIf.First.Offset);
        }

        private void ReconnectBlocks(int index)
        {
            InstructionBlock previousIf = this.instructionToBlockMapping[index];
            for (int i = 1; i < this.switchBlocksToCasesMap[index].Count; i++)
            {
                InstructionBlock currentIf = this.instructionToBlockMapping[this.switchBlocksToCasesMap[index][i]];
                previousIf.RemoveFromSuccessors(previousIf.Successors.Last());
                previousIf.AddToSuccessors(currentIf);

                previousIf = currentIf;
            }
        }

        private bool RemoveOptimizationBlocks()
        {
            // We need them sorted, because this guarantees us that all of them will not have predecessors.
            blocksToBeRemoved.Sort();
            foreach (int index in blocksToBeRemoved)
            {
                this.methodContext.ControlFlowGraph.RemoveBlockAt(this.instructionToBlockMapping[index].Index);
                this.blockExpressions.Remove(index);
            }

            return blocksToBeRemoved.Count > 0;
        }
        
        private class SwitchData
        {
            public SwitchData(VariableReference optimizationVariable, Expression switchExpression)
            {
                this.OptimizationVariable = optimizationVariable;
                this.SwitchExpression = switchExpression;
            }

            public VariableReference OptimizationVariable { get; private set; }
            public Expression SwitchExpression { get; private set; }
        }
    }
}
