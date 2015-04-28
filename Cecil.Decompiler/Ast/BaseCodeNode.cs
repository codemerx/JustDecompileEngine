using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using Telerik.JustDecompiler.Common;

namespace Telerik.JustDecompiler.Ast
{
    public abstract class BaseCodeNode : ICodeNode
    {
        public abstract CodeNodeType CodeNodeType { get; }

        public IEnumerable<Instruction> UnderlyingSameMethodInstructions
        {
            get
            {
                return MergeSortedEnumerables(GetInstructionEnumerables());
            }
        }

        protected IEnumerable<Instruction> MergeSortedEnumerables(IEnumerable<IEnumerable<Instruction>> enumerables)
        {
            List<Instruction> result = new List<Instruction>();
            foreach (IEnumerable<Instruction> enumerable in enumerables)
            {
                result.AddRange(enumerable);
            }
            result.Sort((x, y) => x.Offset.CompareTo(y.Offset));
            return result;
        }

        private IEnumerable<IEnumerable<Instruction>> GetInstructionEnumerables()
        {
            Queue<ICodeNode> queue = new Queue<ICodeNode>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                BaseCodeNode node = queue.Dequeue() as BaseCodeNode;
                yield return node.GetOwnInstructions();

                foreach (ICodeNode child in node.Children)
                {
                    queue.Enqueue(child);
                }
            }
        }

        public abstract IEnumerable<ICodeNode> Children
        {
            get;
        }

        protected abstract IEnumerable<Instruction> GetOwnInstructions();
    }
}
