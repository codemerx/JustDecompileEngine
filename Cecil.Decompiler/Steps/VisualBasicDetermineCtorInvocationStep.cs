using Telerik.JustDecompiler.Decompiler.Inlining;

namespace Telerik.JustDecompiler.Steps
{
    class VisualBasicDetermineCtorInvocationStep : DetermineCtorInvocationStep
    {
        protected override IVariablesToNotInlineFinder VariablesToNotInlineFinder
        {
            get
            {
                return new VisualBasicVariablesToNotInlineFinder();
            }
        }
    }
}
