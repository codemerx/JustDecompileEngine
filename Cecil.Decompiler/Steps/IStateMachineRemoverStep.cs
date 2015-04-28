using System;
using Telerik.JustDecompiler.Languages;

namespace Telerik.JustDecompiler.Steps
{
    interface IStateMachineRemoverStep : IDecompilationStep
    {
        ILanguage Language { get; set; }
    }
}
