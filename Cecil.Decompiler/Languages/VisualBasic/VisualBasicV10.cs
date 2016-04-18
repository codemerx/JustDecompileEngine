using Mono.Cecil;
using Telerik.JustDecompiler.Decompiler;
using Telerik.JustDecompiler.Decompiler.GotoElimination;
using Telerik.JustDecompiler.Languages.VisualBasic;
using Telerik.JustDecompiler.Steps;
using Telerik.JustDecompiler.Steps.SwitchByString;

namespace Telerik.JustDecompiler.Languages
{
    public static partial class LanguageFactory
    {
        private class VisualBasicV10 : VisualBasic, IVisualBasic
        {
            public VisualBasicV10()
            {
                string[] GlobalKeywords =
                {
                    "AddHandler","AddressOf","Alias","And","AndAlso","Ansi","As","Assembly","Auto","Boolean","ByRef","Byte","ByVal","Call","Case","Catch",
                    "CBool","CByte","CChar","CDate","CDec","CDbl","Char","CInt","Class","CLng","CObj","Const","CShort","CSng","CStr","CType","Date",
                    "Decimal","Declare","Default","Delegate","Dim","DirectCast","Do","Double","Each","Else","ElseIf","End","Enum","Erase","Error","Event",
                    "Exit","False","Finally","For","Friend","Function","Get","GetType","GoSub","GoTo","Handles","If","Implements","Imports","In","Inherits",
                    "Integer","Interface","Is","Let","Lib","Like","Long","Loop","Me","Mod","Module","MustInherit","MustOverride","MyBase","MyClass",
                    "Namespace","New","Next","Not","Nothing","NotInheritable","NotOverridable","Object","On","Option","Optional","Or","OrElse","Overloads",
                    "Overridable","Overrides","ParamArray","Preserve","Private","Property","Protected","Public","RaiseEvent","ReadOnly","ReDim","REM",
                    "RemoveHandler","Resume","Return","Select","Set","Shadows","Shared","Short","Single","Static","Step","Stop","String","Structure","Sub",
                    "SyncLock","Then","Throw","To","True","Try","TypeOf","Unicode","Until","Variant","When","While","With","WithEvents","WriteOnly","Xor",
                    "#Const","#ExternalSource","#If","Then","#Else","#Region"
                };

                foreach (string word in GlobalKeywords)
                {
                    this.languageSpecificGlobalKeywords.Add(word);
                }
            }

            public override int Version
            {
                get
                {
                    return 10;
                }
            }

            public override bool SupportsGetterOnlyAutoProperties
            {
                get
                {
                    // TODO: Fix when VB14 is added
                    return true;
                }
            }
            
            internal override IDecompilationStep[] LanguageDecompilationSteps(MethodDefinition method, bool inlineAggressively)
            {
                return new IDecompilationStep[]
                {
                    new TotalGotoEliminationStep(),
                    new AfterGotoCleanupStep(),
                    new RebuildAsyncStatementsStep(),
                    new RebuildYieldStatementsStep() { Language = this },
                    new RemoveDelegateCaching(),
                    // RebuildAnonymousDelegatesStep needs to be executed before the RebuildLambdaExpressions step
                    new RebuildAnonymousDelegatesStep() { Language = this },
                    new RebuildLambdaExpressions() { Language = this, Method = method },

                    new CombinedTransformerStep() { Language = this, Method = method },
                    // new RemoveConditionOnlyVariables(),
                    new MergeUnaryAndBinaryExpression(),
                    new RemoveLastReturn(),
                    new RebuildSwitchByString(),
                    new RebuildForeachStatements(),
                    new RebuildForeachArrayStatements(),
                    new RebuildVBForStatements(),
                    new RebuildDoWhileStatements(),
                    new RebuildLockStatements(),
                    new RebuildFixedStatements(),
                    new RebuildUsingStatements(),
                    new RenameEnumValues(),
                    new FixMethodOverloadsStep(),
                    new DetermineCtorInvocationStep(),
                    new RebuildExpressionTreesStep(),
                    new TransformMemberHandlersStep(),
                    new VBCodePatternsStep(inlineAggressively) { Language = this },
                    // TransformCatchClausesFilterExpressionStep needs to be after VBCodePatternsStep,
                    // because it works only if the TernaryConditionPattern has been applied.
                    new TransformCatchClausesFilterExpressionStep(),
                    new DeduceImplicitDelegates(),
                    new CreateIfElseIfStatementsStep(),
                    new ParenthesizeExpressionsStep(),
                    new RemoveUnusedVariablesStep(),
                    // RebuildCatchClausesFilterStep needs to be before DeclareVariablesOnFirstAssignment and after RemoveUnusedVariablesStep.
                    // RebuildCatchClausesFilterStep contains pattern matching and need to be after TransformCatchClausesFilterExpressionStep.
                    new RebuildCatchClausesFilterStep() { Language = this },
                    new DeclareVariablesOnFirstAssignment(),
                    new DeclareTopLevelVariables(),
                    // There were a lot of issues when trying to merge the SelfAssignment step with the CombinedTransformerStep.
                    new SelfAssignement(),
                    new RenameSplitPropertiesMethodsAndBackingFields(),
                    new RenameVBVariables() { Language = this },
                    new CastEnumsToIntegersStep(),
                    new CastIntegersStep(),
                    new ArrayVariablesStep(),
                    new UnsafeMethodBodyStep(),
                    new DetermineDestructorStep(),
                    new DependsOnAnalysisStep(),
                    new DetermineNotSupportedVBCodeStep(),
                };
            }

            protected override IDecompilationStep[] LanguageFilterMethodDecompilationSteps(MethodDefinition method, bool inlineAggressively)
            {
                return new IDecompilationStep[]
                {
                    new DeclareVariablesOnFirstAssignment(),
                    new DeclareTopLevelVariables(),
                    // There were a lot of issues when trying to merge the SelfAssignment step with the CombinedTransformerStep.
                    new SelfAssignement(),
                    new RenameSplitPropertiesMethodsAndBackingFields(),
                    new RenameVBVariables() { Language = this },
                    new CastEnumsToIntegersStep(),
                    new CastIntegersStep(),
                    new ArrayVariablesStep(),
                    new UnsafeMethodBodyStep(),
                    new DetermineDestructorStep(),
                    new DependsOnAnalysisStep(),
                    new DetermineNotSupportedVBCodeStep(),
                };
            }
        }
    }
}
