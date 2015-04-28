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

// Warning: generated do not edit

using System;
using System.Collections.Generic;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Telerik.JustDecompiler.Ast.Expressions;

namespace Telerik.JustDecompiler.Ast.Statements
{
	public class CatchClause : BasePdbStatement
	{
		public CatchClause()
		{
		}

		public CatchClause(BlockStatement body, TypeReference type, VariableDeclarationExpression variable)
		{
			this.Body = body;
			this.Type = type;
			this.Variable = variable;
		}

		public CatchClause(BlockStatement filter, BlockStatement body)
		{
			this.Filter = filter;
			this.Body = body;
		}

		public BlockStatement Filter { get; set; }

		public BlockStatement Body { get; set; }

		public TypeReference Type { get; set; }

		public VariableDeclarationExpression Variable { get; set; }

        public override IEnumerable<ICodeNode> Children
        {
            get
            {
                if (Variable != null)
                {
                    yield return Variable;
                }
                if (Filter != null)
                {
                    yield return Filter;
                }
                if (Body != null)
                {
                    yield return Body;
                }
            }
        }

		public override CodeNodeType CodeNodeType
		{
			get { return CodeNodeType.CatchClause; }
		}

        public override Statement Clone()
        {
            BlockStatement bodyClone = Body != null ? (BlockStatement)Body.Clone() : null;
            if (Filter != null)
            {
                return new CatchClause((BlockStatement)Filter.Clone(), bodyClone);
            }
            else
            {
                VariableDeclarationExpression variableClone = Variable != null ? (VariableDeclarationExpression)Variable.Clone() : null;
                return new CatchClause(bodyClone, Type, variableClone);
            }
        }

        public override Statement CloneStatementOnly()
        {
            BlockStatement bodyClone = Body != null ? (BlockStatement)Body.CloneStatementOnly() : null;
            if (Filter != null)
            {
                return new CatchClause((BlockStatement)Filter.CloneStatementOnly(), bodyClone);
            }
            else
            {
                VariableDeclarationExpression variableClone = Variable != null ? (VariableDeclarationExpression)Variable.CloneExpressionOnly() : null;
                return new CatchClause(bodyClone, Type, variableClone);
            }
        }
    }
}
