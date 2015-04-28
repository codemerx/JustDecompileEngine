using System;
using Telerik.JustDecompiler.Languages;
using Telerik.JustDecompiler.Decompiler;
using Telerik.JustDecompiler.Decompiler.StateMachines;
using Telerik.JustDecompiler.Cil;
using System.Collections.Generic;

namespace Telerik.JustDecompiler.Steps
{
    abstract class BaseStateMachineRemoverStep : IStateMachineRemoverStep
    {
        protected readonly HashSet<InstructionBlock> toBeRemoved = new HashSet<InstructionBlock>();

        protected MethodSpecificContext moveNextMethodContext;
        protected ControlFlowGraph theCFG;

        public ILanguage Language { get; set; }

        public Ast.Statements.BlockStatement Process(DecompilationContext context, Ast.Statements.BlockStatement body)
        {
            this.moveNextMethodContext = context.MethodContext;
            this.theCFG = this.moveNextMethodContext.ControlFlowGraph;
            moveNextMethodContext.IsMethodBodyChanged = true;

            StateMachineUtilities.FixInstructionConnections(theCFG.Blocks);

            if (!ProcessCFG())
            {
                ((BaseLanguage)Language).StopPipeline();
            }
            return body;
        }

        protected abstract bool ProcessCFG();
    }
}
