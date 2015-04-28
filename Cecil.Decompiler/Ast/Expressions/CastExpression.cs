#region license
//
//	(C) 2005 - 2007 db4objects Inc. http://www.db4o.com
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

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Telerik.JustDecompiler.Ast.Expressions
{
    public class CastExpression : Expression, IDynamicTypeContainer
    {
        public CastExpression(Expression expression, TypeReference targetType, IEnumerable<Instruction> instructions, MemberReference unresolvedReferenceForAmbiguousCastToObject = null)
            :base(instructions)
		{
			this.Expression = expression;
			this.TargetType = targetType;

            this.UnresolvedReferenceForAmbiguousCastToObject = unresolvedReferenceForAmbiguousCastToObject;

            DetermineIsChecked();
		}

        internal bool IsExplicitInterfaceCast { get; set; }

        public MemberReference UnresolvedReferenceForAmbiguousCastToObject { get; internal set; }

        private void DetermineIsChecked()
        {
            foreach (Instruction instruction in this.instructions)
            {
                switch (instruction.OpCode.Code)
                {
                    case Code.Conv_Ovf_I1_Un:
                    case Code.Conv_Ovf_I2_Un:
                    case Code.Conv_Ovf_I4_Un:
                    case Code.Conv_Ovf_I8_Un:
                    case Code.Conv_Ovf_U1_Un:
                    case Code.Conv_Ovf_U2_Un:
                    case Code.Conv_Ovf_U4_Un:
                    case Code.Conv_Ovf_U8_Un:
                    case Code.Conv_Ovf_I_Un:
                    case Code.Conv_Ovf_U_Un:
                    case Code.Conv_Ovf_I1:
                    case Code.Conv_Ovf_U1:
                    case Code.Conv_Ovf_I2:
                    case Code.Conv_Ovf_U2:
                    case Code.Conv_Ovf_I4:
                    case Code.Conv_Ovf_U4:
                    case Code.Conv_Ovf_I8:
                    case Code.Conv_Ovf_U8:
                    case Code.Conv_Ovf_I:
                    case Code.Conv_Ovf_U:
                        this.IsChecked = true;
                        return;
                }
            }

            this.IsChecked = false;
        }

        public override IEnumerable<ICodeNode> Children
        {
            get { yield return Expression; }
        }

		public override bool Equals(Expression other)
		{
            if (!(other is CastExpression))
            {
                return false;
            }
            if (this.TargetType.FullName != (other as CastExpression).TargetType.FullName)
            {
                return false;
            }
            return this.Expression.Equals((other as CastExpression).Expression);
        }

        public override Expression Clone()
        {
			CastExpression result = new CastExpression(Expression.Clone(), TargetType, instructions) 
										{
                                            IsChecked = this.IsChecked,
                                            IsExplicitInterfaceCast = this.IsExplicitInterfaceCast,
                                            UnresolvedReferenceForAmbiguousCastToObject = this.UnresolvedReferenceForAmbiguousCastToObject
                                        };
			return result;
        }

        public override Expression CloneExpressionOnly()
        {
			CastExpression result = new CastExpression(Expression.CloneExpressionOnly(), TargetType, null) 
										{
                                            IsChecked = this.IsChecked,
                                            IsExplicitInterfaceCast = this.IsExplicitInterfaceCast,
                                            UnresolvedReferenceForAmbiguousCastToObject = this.UnresolvedReferenceForAmbiguousCastToObject
                                        };
            return result;
        }

		public Expression Expression { get; set; }

        public TypeReference TargetType { get; set; }

		public bool IsChecked { get; private set; }

        public override TypeReference ExpressionType
        {
            get
            {
                return TargetType;
            }
            set
            {
                TargetType = value;
            }
        }

        public override bool HasType
        {
            get
            {
                return true;
            }
        }

        public override CodeNodeType CodeNodeType
        {
            get { return CodeNodeType.CastExpression; }
        }

        public bool IsDynamic
        {
            get
            {
                return DynamicPositioningFlags != null;
            }
        }

        public bool[] DynamicPositioningFlags { get; set; }

        TypeReference IDynamicTypeContainer.DynamicContainingType
        {
            get
            {
                return this.TargetType;
            }
        }
    }
}