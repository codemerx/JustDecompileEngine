using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using Telerik.JustDecompiler.Cil;
using Mono.Collections.Generic;

namespace Telerik.JustDecompiler.Decompiler.StateMachines
{
    /// <summary>
    /// Removes the blocks that check whether a finally handler should be executed.
    /// </summary>
    /// <remarks>
    /// The newer versions of the compiler no longer destroy the try/finally guarded blocks when creating the state machine.
    /// Instead, a new boolean variable, named doFinallyBodies, is introduced. Also blocks, that check the value of this variable,
    /// are inserted at the begining of each finally handler. If doFinallyBodies is true then the handler is executed,
    /// otherwise the block branches to the endfinally instruction at the end of the finally handler.
    /// The doFinallyBodies variable is set to true at the first block of the MoveNext method. Whenever a state should terminate (e.g. yield return),
    /// the variable is set to false in order to not execute the finally handlers when leaving the guarded blocks.
    /// 
    /// The purpose of this class is to detach the compiler generated blocks at the begining of the finally handlers and then mark them for removal.
    /// </remarks>
    class StateMachineDoFinallyCheckRemover
    {
        private readonly HashSet<InstructionBlock> toBeRemoved = new HashSet<InstructionBlock>();

        private readonly ControlFlowGraph theCFG;
        private readonly Collection<VariableDefinition> methodVariables;

        private VariableReference doFinallyVariable;

        /// <summary>
        /// Gets the doFinallyBodies variable.
        /// </summary>
        public VariableReference DoFinallyVariable
        {
            get
            {
                return doFinallyVariable;
            }
        }

        /// <summary>
        /// Gets the blocks that were marked for removal during the cleaning of the CFG.
        /// </summary>
        public HashSet<InstructionBlock> BlocksMarkedForRemoval
        {
            get
            {
                return toBeRemoved;
            }
        }

        public StateMachineDoFinallyCheckRemover(MethodSpecificContext methodContext)
        {
            this.methodVariables = methodContext.Body.Variables;
            this.theCFG = methodContext.ControlFlowGraph;
        }

        /// <summary>
        /// Finds and marks the do-finally-check blocks for removal.
        /// </summary>
        /// <returns>False if there is no doFinallyBodies variable, true - otherwise.</returns>
        public bool MarkFinallyConditionsForRemoval()
        {
            if (GetDoFinallyVariable())
            {
                foreach (ExceptionHandler exHandler in theCFG.RawExceptionHandlers)
                {
                    InstructionBlock finallyEntry;
                    if (exHandler.HandlerType == ExceptionHandlerType.Finally &&
                        theCFG.InstructionToBlockMapping.TryGetValue(exHandler.HandlerStart.Offset, out finallyEntry) &&
                        IsDoFinallyCheck(finallyEntry))
                    {
                        toBeRemoved.Add(finallyEntry);
                        finallyEntry.Successors = new InstructionBlock[0];
                    }
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the doFinallyBodies variable from the first block of the MoveNext method.
        /// </summary>
        /// <returns>True on success, otherwise false.</returns>
        private bool GetDoFinallyVariable()
        {
            Instruction entry = theCFG.Blocks[0].First;
            if (entry.OpCode.Code != Code.Ldc_I4_1)
            {
                return false;
            }

            return StateMachineUtilities.TryGetVariableFromInstruction(entry.Next, methodVariables, out doFinallyVariable);
        }

        /// <summary>
        /// Checks whether <paramref name="theBlock"/> is a do-finally-check block.
        /// </summary>
        /// <remarks>
        /// Pattern:
        /// (nop) - sequence of nop blocks
        /// ldloc* doFinallyVariable
        /// brfalse* - branch to endfinally - this check varies between compilers
        /// original handler:
        /// ...............
        /// </remarks>
        /// <param name="theBlock"></param>
        /// <returns></returns>
        private bool IsDoFinallyCheck(InstructionBlock theBlock)
        {
            Instruction currentInstruction = theBlock.First;
            while (currentInstruction.OpCode.Code == Code.Nop)
            {
                if (currentInstruction == theBlock.Last)
                {
                    return false;
                }
                currentInstruction = currentInstruction.Next;
            }

            VariableReference loadedVariable;
            if (currentInstruction == theBlock.Last ||
                !StateMachineUtilities.TryGetVariableFromInstruction(currentInstruction, methodVariables, out loadedVariable) ||
                loadedVariable != doFinallyVariable)
            {
                return false;
            }
            currentInstruction = currentInstruction.Next;

            if (currentInstruction.OpCode.Code == Code.Brfalse || currentInstruction.OpCode.Code == Code.Brfalse_S)
            {
                return true;
            }

            return IsCSharpDebugCheck(theBlock, currentInstruction) || IsVisualBasicDebugCheck(theBlock, currentInstruction);
        }

        /// <summary>
        /// Checks whether the specified block ends as a do-finally-check block generated by the C# compiler in debug mode.
        /// </summary>
        /// <remarks>
        /// Instead of:
        /// brfalse* endfinally
        /// 
        /// ldc.i4.0
        /// ..............
        /// brtrue* endfinally
        /// </remarks>
        /// <param name="theBlock"></param>
        /// <param name="currentInstruction"></param>
        /// <returns></returns>
        private bool IsCSharpDebugCheck(InstructionBlock theBlock, Instruction currentInstruction)
        {
            return currentInstruction.OpCode.Code == Code.Ldc_I4_0 && (theBlock.Last.OpCode.Code == Code.Brtrue || theBlock.Last.OpCode.Code == Code.Brtrue_S);
        }

        /// <summary>
        /// Checks whether the specified block ends as a do-finally-check block generated by the VB compiler in debug mode.
        /// </summary>
        /// <remarks>
        /// Instead of:
        /// brfalse* endfinally
        /// 
        /// stloc* someVariable
        /// ldloc* someVariable
        /// brfalse* endfinally
        /// </remarks>
        /// <param name="theBlock"></param>
        /// <param name="currentInstruction"></param>
        /// <returns></returns>
        private bool IsVisualBasicDebugCheck(InstructionBlock theBlock, Instruction currentInstruction)
        {
            VariableReference storingVariable;
            if(currentInstruction == theBlock.Last ||
                !StateMachineUtilities.TryGetVariableFromInstruction(currentInstruction, methodVariables, out storingVariable))
            {
                return false;
            }

            currentInstruction = currentInstruction.Next;
            VariableReference loadingVariable;
            if (currentInstruction == theBlock.Last ||
                !StateMachineUtilities.TryGetVariableFromInstruction(currentInstruction, methodVariables, out loadingVariable) ||
                storingVariable != loadingVariable)
            {
                return false;
            }

            currentInstruction = currentInstruction.Next;
            return currentInstruction == theBlock.Last && (currentInstruction.OpCode.Code == Code.Brfalse || currentInstruction.OpCode.Code == Code.Brfalse_S);
        }
    }
}
