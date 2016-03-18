using System.Collections.Generic;
using Mono.Cecil;
using Telerik.JustDecompiler.Steps;
using Telerik.JustDecompiler.Decompiler.GotoElimination;
using Telerik.JustDecompiler.Steps.SwitchByString;

namespace Telerik.JustDecompiler.Languages.CSharp
{
    public class CSharpV5 : CSharpV4
    {
        new protected static HashSet<string> languageSpecificGlobalKeywords;
        new protected static HashSet<string> languageSpecificContextualKeywords;

        static CSharpV5()
        {
            CSharpV5.languageSpecificGlobalKeywords = new HashSet<string>(CSharpV4.languageSpecificGlobalKeywords);
            CSharpV5.languageSpecificContextualKeywords = new HashSet<string>(CSharpV4.languageSpecificContextualKeywords);
        }

        public override string Name
        {
            get
            {
                return "C#5";
            }
        }

        public override bool IsLanguageKeyword(string word)
        {
            return base.IsLanguageKeyword(word, CSharpV5.languageSpecificGlobalKeywords, CSharpV5.languageSpecificContextualKeywords);
        }

        public override bool IsGlobalKeyword(string word)
        {
            return IsGlobalKeyword(word, CSharpV5.languageSpecificGlobalKeywords);
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
                new DetermineCtorInvocationStep(),
                new DeduceImplicitDelegates(),
                new RebuildLinqQueriesStep(),
                new CreateIfElseIfStatementsStep(),
                new ParenthesizeExpressionsStep(),
                new RemoveUnusedVariablesStep(),
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
