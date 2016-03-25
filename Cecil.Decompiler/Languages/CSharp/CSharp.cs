#region license
//
//	(C) 2007 - 2008 Novell, Inc. http://www.novell.com
//	(C) 2007 - 2008 Jb Evain http://evain.net
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using Mono.Cecil;
using System;
using System.Collections.Generic;
using Telerik.JustDecompiler.Decompiler;
using Telerik.JustDecompiler.Steps;

namespace Telerik.JustDecompiler.Languages.CSharp
{
    public class CSharp : BaseLanguage
    {
        new protected static HashSet<string> languageSpecificGlobalKeywords;
        new protected static HashSet<string> languageSpecificContextualKeywords;
        private static Dictionary<string, string> operators;

        public override bool IsGlobalKeyword(string word)
        {
            return IsGlobalKeyword(word, CSharp.languageSpecificGlobalKeywords);
        }

        static CSharp()
        {
            CSharp.languageSpecificGlobalKeywords = new HashSet<string>();
            CSharp.languageSpecificContextualKeywords = new HashSet<string>();
            CSharp.operators = new Dictionary<string, string>();
            InitializeOperators();
        }

        private static void InitializeOperators()
        {
            //taken from ECMA-355 documentation
            //TODO: test all of them
            string[,] operatorPairs = { 
                                          //unary operators
                                          { "Decrement", "--" }, { "Increment", "++" }, { "UnaryNegation", "-" }, { "UnaryPlus", "+" }, { "LogicalNot", "!" },
                                          {"OnesComplement","~"}, {"True", "true"}, {"False", "false"},
                                          //{ "AddressOf", "&" },{"PointerDereference","*"}, to test those we will need to run unsafe code
                                          //binary operators
                                          {"Addition","+"},{"Subtraction","-"},{"Multiply","*"},{"Division","/"},{"Modulus","%"},{"ExclusiveOr","^"},
                                          {"BitwiseAnd","&"},{"BitwiseOr","|"},
                                          //{"LogicalAnd","&&"},{"LogicalOr","||"},
                                          {"LeftShift","<<"},{"RightShift",">>"},
                                          {"Equality","=="},{"GreaterThan",">"},{"LessThan","<"},{"Inequality","!="},{"GreaterThanOrEqual",">="},{"LessThanOrEqual","<="},
                                          {"MemberSelection","->"},{"PointerToMemberSelection","->*"},{"Comma",","},
                                          //those can't be redefined, so no point looking for them
                                          //{"RightShiftAssignment",">>="},{"MultiplicationAssignment","*="},
                                          //{"SubtractionAssignment","-="},{"ExclusiveOrAssignment","^="},{"LeftShiftAssignment","<<="},{"ModulusAssignment","%="},
                                          //{"AdditionAssignment","+="},{"BitwiseAndAssignment","&="},{"BitwiseOrAssignment","|="},{"DivisionAssignment","/="},
                                          //other
                                          {"Implicit",""},{"Explicit",""}, //those are for imlicit/explicit type casts
                                      };
            for (int row = 0; row < operatorPairs.GetLength(0); row++)
            {
                operators.Add(operatorPairs[row, 0], operatorPairs[row, 1]);
            }
        }

        public override string FloatingLiteralsConstant
        {
            get
            {
                return "f";
            }
        }

        public override string Name
        {
            get
            {
                return "C#" + this.Version;
            }
        }

        public override int Version
        {
            get
            {
                return 0;
            }
        }

        public override string VSCodeFileExtension
        {
            get
            {
                return ".cs";
            }
        }

        public override string VSProjectFileExtension
        {
            get
            {
                return ".csproj";
            }
        }

		public override string EscapeSymbolBeforeKeyword
		{
			get
			{
				return "@";
			}
		}

		public override string EscapeSymbolAfterKeyword
		{
			get
			{
				return "";
			}
		}

        public override string CommentLineSymbol
        {
            get { return "//"; }
        }

        public override string DocumentationLineStarter
        {
            get { return "///"; }
        }

        public override ILanguageWriter GetWriter(IFormatter formatter, IExceptionFormatter exceptionFormatter, bool writeExceptionsAsComments)
        {
            return new CSharpWriter(this, formatter, exceptionFormatter, writeExceptionsAsComments);
        }

        public override IAssemblyAttributeWriter GetAssemblyAttributeWriter(IFormatter formatter, IExceptionFormatter exceptionFormatter, bool writeExceptionsAsComments)
        {
            return new CSharpAssemblyAttributeWriter(this, formatter, exceptionFormatter, writeExceptionsAsComments);
        }

        public static ILanguage GetLanguage(CSharpVersion version)
        {
            switch (version)
            {
                case CSharpVersion.None:
                    return new CSharp();
                case CSharpVersion.V1:
                case CSharpVersion.V2:
                case CSharpVersion.V3:
                case CSharpVersion.V4:
                case CSharpVersion.V5:
                    return new CSharpV5();
                case CSharpVersion.V6:
                    return new CSharpV6();
                default:
                    throw new ArgumentException();
            }
        }

        public override bool IsLanguageKeyword(string word)
        {
            return base.IsLanguageKeyword(word, CSharp.languageSpecificGlobalKeywords, CSharp.languageSpecificContextualKeywords);
        }

		public override bool IsOperatorKeyword(string @operator)
		{
			return false;
		}

        protected override string GetCommentLine()
        {
            return @"//";
        }

        public override bool TryGetOperatorName(string operatorName, out string languageOperator)
        {
            bool result = operators.TryGetValue(operatorName, out languageOperator);
            return result;
        }

        public override bool SupportsGetterOnlyAutoProperties
        {
            get
            {
                return false;
            }
        }

        public override bool SupportsInlineInitializationOfAutoProperties
        {
            get
            {
                return false;
            }
        }

        public override bool SupportsExceptionFilters
        {
            get
            {
                return false;
            }
        }

        public override DecompilationPipeline CreatePipeline(MethodDefinition method)
        {
            DecompilationPipeline result = base.CreatePipeline(method);
            result.AddSteps(CSharpDecompilationSteps(method, false));
            return result;
        }

        public override DecompilationPipeline CreatePipeline(MethodDefinition method, DecompilationContext context)
        {
            return CreatePipelineInternal(method, context, false);
        }

        public override DecompilationPipeline CreateLambdaPipeline(MethodDefinition method, DecompilationContext context)
        {
            return CreatePipelineInternal(method, context, true);
        }

        // This pipeline is used by the PropertyDecompiler to finish the decompilation of properties, which are partially decompiled
        // using the steps from the IntermediateRepresenationPipeline.
        public override BlockDecompilationPipeline CreatePropertyPipeline(MethodDefinition method, DecompilationContext context)
        {
            return new BlockDecompilationPipeline(CSharpDecompilationSteps(method, false), context);
        }

        private DecompilationPipeline CreatePipelineInternal(MethodDefinition method, DecompilationContext context, bool inlineAggressively)
        {
            DecompilationPipeline result = base.CreatePipeline(method, context);
            result.AddSteps(CSharpDecompilationSteps(method, inlineAggressively));
            return result;
        }

        internal virtual IDecompilationStep[] CSharpDecompilationSteps(MethodDefinition method, bool inlineAggressively)
        {
            return new IDecompilationStep[0];
        }
    }
}