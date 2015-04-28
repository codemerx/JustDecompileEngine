using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Mono.Cecil;
using Telerik.JustDecompiler.Common;
using Telerik.JustDecompiler.Decompiler;
using Telerik.JustDecompiler.Decompiler.DefineUseAnalysis;
using Telerik.JustDecompiler.Decompiler.LogicFlow;
using Telerik.JustDecompiler.Steps;
using Telerik.JustDecompiler.Decompiler.GotoElimination;
using System.Text.RegularExpressions;

namespace Telerik.JustDecompiler.Languages
{
    public abstract class BaseLanguage : ILanguage
    {
        protected static HashSet<string> languageSpecificGlobalKeywords;

        protected static HashSet<string> languageSpecificContextualKeywords;

        public virtual string FloatingLiteralsConstant
        {
            get
            {
                return "";
            }
        }

        public static DecompilationPipeline IntermediateRepresenationPipeline
        {
            get
            {
				return new DecompilationPipeline(new RemoveUnreachableBlocksStep(),
												 new StackUsageAnalysis(),
												 new ExpressionDecompilerStep(),
												 new ManagedPointersRemovalStep(),
												 new VariableAssignmentAnalysisStep(),
												 new LogicalFlowBuilderStep(),
												 new FollowNodeLoopCleanUpStep(),
												 new StatementDecompilerStep(),
												 new MapUnconditionalBranchesStep());
			}
        }

        private bool writeLargeNumbersInHex = true;

        public bool IsStopped
        {
            get;
            private set;
        }

        public bool WriteLargeNumbersInHex
        {
            get
            {
                return writeLargeNumbersInHex;
            }
            set
            {
                writeLargeNumbersInHex = value;
            }
        }

        public abstract string Name { get; }

		public abstract string EscapeSymbolBeforeKeyword { get; }

		public abstract string EscapeSymbolAfterKeyword { get; }

        public abstract string CommentLineSymbol { get; }

        public abstract string DocumentationLineStarter { get; }

		public virtual StringComparer IdentifierComparer
		{
			get
			{
				return StringComparer.Ordinal;
			}
		}

        public string ReplaceInvalidCharactersInIdentifier(string identifier)
        {
            //sanity checks
            if (identifier == null)
            {
                return null;
            }
            if (identifier == string.Empty)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();

            char firstCharacter = identifier[0];
            if (IsValidIdentifierFirstCharacter(firstCharacter))
            {
                sb.Append(firstCharacter);
            }
            else
            {
                sb.Append(string.Format("u{0:x4}", (int)firstCharacter));
            }
            for (int i = 1; i < identifier.Length; i++)
            {
                char currentChar = identifier[i];
                if (IsValidIdentifierCharacter(currentChar))
                {
                    sb.Append(currentChar);
                }
                else
                {
                    sb.Append(string.Format("u{0:x4}", (int)currentChar));
                }
            }
            return sb.ToString();
        }

        public bool IsValidIdentifier(string identifier)
        {
            /// The pattern is taken from the ECMA-334 standart, 9.4.2 Identifiers (page 92).
            /// Although the regex covers the C# identifiers, the rules for VB are the same.
            /// No care is taken for escape sequences in our case.
            return identifier.IsValidIdentifier();
        }

        public bool IsValidIdentifierCharacter(char currentChar)
        {
            /// The pattern is taken from the ECMA-334 standart, 9.4.2 Identifiers (page 92).
            /// Although the check covers the C# identifiers, the rules for VB are the same.
            if (IsValidIdentifierFirstCharacter(currentChar))
            {
                return true;
            }

            UnicodeCategory unicodeCategory = char.GetUnicodeCategory(currentChar);

            if (unicodeCategory == UnicodeCategory.NonSpacingMark || // class Mn
                unicodeCategory == UnicodeCategory.SpacingCombiningMark || // class Mc
                unicodeCategory == UnicodeCategory.DecimalDigitNumber || // class Nd
                unicodeCategory == UnicodeCategory.ConnectorPunctuation || // class Pc
                unicodeCategory == UnicodeCategory.Format)                  // class Cf
            {
                return true;
            }
            return false;
        }

        public bool IsValidIdentifierFirstCharacter(char firstCharacter)
        {
            /// The pattern is taken from the ECMA-334 standart, 9.4.2 Identifiers (page 92).
            /// Although the check covers the C# identifiers, the rules for VB are the same.
            if (firstCharacter == '_')
            {
                return true;
            }

            UnicodeCategory unicodeCategory = char.GetUnicodeCategory(firstCharacter);

            if (unicodeCategory == UnicodeCategory.LowercaseLetter || // class Ll
                unicodeCategory == UnicodeCategory.UppercaseLetter || // class Lu
                unicodeCategory == UnicodeCategory.TitlecaseLetter || // class Lt
                unicodeCategory == UnicodeCategory.ModifierLetter || // class Lm
                unicodeCategory == UnicodeCategory.OtherLetter || // class Lo
                unicodeCategory == UnicodeCategory.LetterNumber)	  // class Nl
            {
                return true;
            }

            return false;
        }

        public abstract ILanguageWriter GetWriter(IFormatter formatter, IExceptionFormatter exceptionFormatter, bool writeExceptionsAsComments);

        public abstract IAssemblyAttributeWriter GetAssemblyAttributeWriter(IFormatter formatter, IExceptionFormatter exceptionFormatter, bool writeExceptionsAsComments);

        public virtual DecompilationPipeline CreatePipeline(MethodDefinition method)
        {
            DecompilationPipeline result = new DecompilationPipeline();
            result.AddSteps(IntermediateRepresenationPipeline.Steps);
            return result;
        }

        public virtual DecompilationPipeline CreatePipeline(MethodDefinition method, DecompilationContext context)
        {
            DecompilationPipeline result = new DecompilationPipeline(IntermediateRepresenationPipeline.Steps, context);
            return result;
        }

        public virtual DecompilationPipeline CreateLambdaPipeline(MethodDefinition method, DecompilationContext context)
        {
            throw new NotSupportedException();
        }

        public void StopPipeline()
        {
            IsStopped = true;
        }

        public abstract bool IsLanguageKeyword(string word);

        public abstract bool IsGlobalKeyword(string word);

		public abstract bool IsOperatorKeyword(string @operator);

        protected virtual bool IsLanguageKeyword(string word, HashSet<string> globalKeywords, HashSet<string> contextKeywords)
        {
            bool result = globalKeywords.Contains(word) || contextKeywords.Contains(word);
            return result;
        }

        protected virtual bool IsGlobalKeyword(string word, HashSet<string> globalKeywords)
        {
            return globalKeywords.Contains(word);
        }

        public virtual string EscapeWord(string word)
        {
            string escapedWord = EscapeSymbolBeforeKeyword + word + EscapeSymbolAfterKeyword;
            return escapedWord;
        }

		public virtual bool IsEscapedWord(string word)
		{
			return word.StartsWith(EscapeSymbolBeforeKeyword) && word.EndsWith(EscapeSymbolAfterKeyword);
		}

		public virtual bool IsEscapedWord(string escapedWord, string originalWord)
		{
			return escapedWord == EscapeWord(originalWord);
		}

		public virtual string GetExplicitName(IMemberDefinition member)
		{
			return member.Name;
		}

        public abstract string VSCodeFileExtension { get; }

        public abstract string VSProjectFileExtension { get; }

        public string CommentLines(string text)
        {
            string result = CommentLines(text, GetCommentLine());
            return result;
        }

        protected abstract string GetCommentLine();

        private string CommentLines(string text, string lineCommentString)
        {
            StringBuilder sb = new StringBuilder();
            StringReader reader = new StringReader(text);
            using (reader)
            {
                string currentLine = reader.ReadLine();
                while (currentLine != null)
                {
                    sb.AppendLine(string.Format("{0} {1}", lineCommentString, currentLine));
                    currentLine = reader.ReadLine();
                }
            }
            return sb.ToString();
        }

        public virtual bool TryGetOperatorName(string operatorName, out string languageOperator)
        {
            languageOperator = null;
            return false;
        }

        public bool Equals(ILanguage other)
        {
            if (other == null)
            {
                return false;
            }
            return string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

		public virtual bool HasOutKeyword
		{
			get
			{
				return true;
			}
		}
    }
}