using Telerik.JustDecompiler.Decompiler.Inlining;
using Telerik.JustDecompiler.Languages;

namespace Telerik.JustDecompiler.Decompiler
{
    internal class VisualBasicExpressionDecompilerStep : ExpressionDecompilerStep
    {
        public VisualBasicExpressionDecompilerStep(ILanguage language)
            : base(language)
        {
        }

        protected override IVariablesToNotInlineFinder VariablesToNotInlineFinder
        {
            get
            {
                return new VisualBasicVariablesToNotInlineFinder();
            }
        }
    }
}
