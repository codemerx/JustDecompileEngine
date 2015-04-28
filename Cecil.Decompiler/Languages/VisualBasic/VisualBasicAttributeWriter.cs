using System;
using Mono.Cecil;
using Mono.Cecil.Extensions;

namespace Telerik.JustDecompiler.Languages.VisualBasic
{
	public class VisualBasicAttributeWriter : AttributeWriter
	{
		public VisualBasicAttributeWriter(NamespaceImperativeLanguageWriter writer) : base(writer)
		{
		}

		protected override string OpeningBracket
		{
			get	{ return "<"; }
		}

		protected override string ClosingBracket
		{
			get	{ return ">"; }
		}

		protected override string EqualsSign
		{
			get	{ return ":="; }
		}

        protected override string ParameterAttributeSeparator
        {
            get { return " "; }
        }

		protected override Mono.Cecil.CustomAttribute GetOutAttribute(ParameterDefinition parameter)
		{
			if (parameter.IsOutParameter())
			{
				return GetInOrOutAttribute(parameter, false);
			}

			return base.GetOutAttribute(parameter);
		}
	}
}