using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using Telerik.JustDecompiler.Cil;

namespace Telerik.JustDecompiler.Decompiler.StateMachines
{
    internal class AsyncMoveNextMethodAnalyzer
    {
        private ControlFlowGraph theCFG;
        private IList<VariableDefinition> methodVariables;
        private FieldDefinition stateField;

        private VariableReference stateVariable;
        private VariableReference doFinallyVariable;

        public AsyncMoveNextMethodAnalyzer(MethodSpecificContext moveNextMethodContext, FieldDefinition stateField)
        {
            this.theCFG = moveNextMethodContext.ControlFlowGraph;
            this.methodVariables = moveNextMethodContext.Variables;
            this.stateField = stateField;

            if (GetDoFinallyVariable())
            {
                this.StateMachineVersion = AsyncStateMachineVersion.V1;
            }
            else
            {
                GetStateVariable();
                this.StateMachineVersion = AsyncStateMachineVersion.V2;
            }
        }

        public VariableReference DoFinallyVariable
        {
            get
            {
                return this.doFinallyVariable;
            }
        }

        public VariableReference StateVariable
        {
            get
            {
                return this.stateVariable;
            }
        }

        public AsyncStateMachineVersion StateMachineVersion { get; private set; }

        private void GetStateVariable()
        {
            Instruction current = theCFG.Blocks[0].First;
            if (current.OpCode.Code != Code.Ldarg_0)
            {
                return;
            }

            current = current.Next;
            if (current.OpCode.Code != Code.Ldfld)
            {
                return;
            }

            FieldReference loadedField = current.Operand as FieldReference;
            if (loadedField == null || loadedField.Resolve() != this.stateField)
            {
                return;
            }

            current = current.Next;
            StateMachineUtilities.TryGetVariableFromInstruction(current, methodVariables, out stateVariable);
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
    }
}
