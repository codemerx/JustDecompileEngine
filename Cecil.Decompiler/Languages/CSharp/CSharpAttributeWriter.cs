using System;

namespace Telerik.JustDecompiler.Languages.CSharp
{
    public class CSharpAttributeWriter : AttributeWriter
    {
        public CSharpAttributeWriter(NamespaceImperativeLanguageWriter writer)
            : base(writer)
        {
            attributesNotToShow.Add("System.Runtime.CompilerServices.DynamicAttribute");
            attributesNotToShow.Add("System.Runtime.CompilerServices.ExtensionAttribute");
        }

        protected override string OpeningBracket
        {
            get { return "["; }
        }

        protected override string ClosingBracket
        {
            get { return "]"; }
        }

        protected override string EqualsSign
        {
            get { return "="; }
        }

        protected override string ParameterAttributeSeparator
        {
            get { return string.Empty; }
        }
    }
}