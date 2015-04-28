using System;

namespace Telerik.JustDecompiler.Steps.CodePatterns
{
    class VariableInliningPatternAggressive : VariableInliningPattern
    {
        public VariableInliningPatternAggressive(CodePatternsContext patternsContext, Telerik.JustDecompiler.Decompiler.MethodSpecificContext methodContext)
            :base(patternsContext, methodContext)
        {
        }

        protected override bool ShouldInlineAggressively(Mono.Cecil.Cil.VariableDefinition variable)
        {
            return true;
        }
    }
}
