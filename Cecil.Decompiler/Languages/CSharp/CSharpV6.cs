using Mono.Cecil;
using System.Collections.Generic;
using Telerik.JustDecompiler.Decompiler;
using Telerik.JustDecompiler.Decompiler.GotoElimination;
using Telerik.JustDecompiler.Steps;
using Telerik.JustDecompiler.Steps.SwitchByString;

namespace Telerik.JustDecompiler.Languages.CSharp
{
    public class CSharpV6 : CSharpV5
    {
        public override int Version
        {
            get
            {
                return 6;
            }
        }
        
        public override bool SupportsGetterOnlyAutoProperties
        {
            get
            {
                return true;
            }
        }

        public override bool SupportsInlineInitializationOfAutoProperties
        {
            get
            {
                return true;
            }
        }

        public override bool SupportsExceptionFilters
        {
            get
            {
                return true;
            }
        }

        public override BlockDecompilationPipeline CreateFilterMethodPipeline(MethodDefinition method, DecompilationContext context)
        {
            return new BlockDecompilationPipeline(CSharpFilterMethodDecompilationSteps(method, false), context);
        }

        internal override IDecompilationStep[] CSharpDecompilationSteps(MethodDefinition method, bool inlineAggressively)
        {
            return new IDecompilationStep[]
            {
                new OutParameterAssignmentAnalysisStep(),
                new RebuildAsyncStatementsStep(),
                new RebuildYieldStatementsStep() { Language = this },
                new RemoveDelegateCaching(),
                // RebuildAnonymousDelegatesStep needs to be executed before the RebuildLambdaExpressions step
                new RebuildAnonymousDelegatesStep() { Language = this },
                new RebuildLambdaExpressions() { Language = this, Method = method },
                new ResolveDynamicVariablesStep(),
                new GotoCancelation(),
                new CombinedTransformerStep() { Language = this, Method = method },
                new MergeUnaryAndBinaryExpression(),
                new RemoveLastReturn(),
                new RebuildSwitchByString(),
                new RebuildForeachStatements(),
                new RebuildForeachArrayStatements(),
                new RebuildForStatements(),
                new RebuildLockStatements(),
                new RebuildFixedStatements(),
                new RebuildUsingStatements(),
                new RenameEnumValues(),
                new FixMethodOverloadsStep(),
                new RebuildExpressionTreesStep(),
                new TransformMemberHandlersStep(),
                new CodePatternsStep(inlineAggressively) { Language = this },
                // TransformCatchClausesFilterExpressionStep needs to be after CodePatternsStep,
                // because it works only if the TernaryConditionPattern has been applied.
                new TransformCatchClausesFilterExpressionStep(),
                new DetermineCtorInvocationStep(),
                new DeduceImplicitDelegates(),
                new RebuildLinqQueriesStep(),
                new CreateIfElseIfStatementsStep(),
                new ParenthesizeExpressionsStep(),
                new RemoveUnusedVariablesStep(),
                // RebuildCatchClausesFilterStep needs to be before DeclareVariablesOnFirstAssignment and after RemoveUnusedVariablesStep.
                // RebuildCatchClausesFilterStep contains pattern matching and need to be after TransformCatchClausesFilterExpressionStep.
                new RebuildCatchClausesFilterStep() { Language = this },
                new DeclareVariablesOnFirstAssignment(),
                new DeclareTopLevelVariables(),
                new AssignOutParametersStep(),
                // There were a lot of issues when trying to merge the SelfAssignment step with the CombinedTransformerStep.
                new SelfAssignement(),
                new RenameSplitPropertiesMethodsAndBackingFields(),
                new RenameVariables() { Language = this },
                new CastEnumsToIntegersStep(),
                new CastIntegersStep(),
                new ArrayVariablesStep(),
                new CaseGotoTransformerStep(),
                new UnsafeMethodBodyStep(),
                new DetermineDestructorStep(),
                // DependsOnAnalysisStep must be always last step, because it make analysis on the final decompilation result.
				new DependsOnAnalysisStep(),
            };
        }

        private IDecompilationStep[] CSharpFilterMethodDecompilationSteps(MethodDefinition method, bool inlineAggressively)
        {
            return new IDecompilationStep[]
            {
                new DeclareVariablesOnFirstAssignment(),
                new DeclareTopLevelVariables(),
                new AssignOutParametersStep(),
                // There were a lot of issues when trying to merge the SelfAssignment step with the CombinedTransformerStep.
                new SelfAssignement(),
                new RenameSplitPropertiesMethodsAndBackingFields(),
                new RenameVariables() { Language = this },
                new CastEnumsToIntegersStep(),
                new CastIntegersStep(),
                new ArrayVariablesStep(),
                new CaseGotoTransformerStep(),
                new UnsafeMethodBodyStep(),
                new DetermineDestructorStep(),
                // DependsOnAnalysisStep must be always last step, because it make analysis on the final decompilation result.
				new DependsOnAnalysisStep(),
            };
        }
    }
}
