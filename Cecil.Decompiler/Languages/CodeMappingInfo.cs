using Mono.Cecil.Cil;
using System.Collections.Generic;
using Telerik.JustDecompiler.Ast;

namespace Telerik.JustDecompiler.Languages
{
    public class CodeMappingInfo
    {
        private Dictionary<ICodeNode, OffsetSpan> nodeToCodeMap;

        public CodeMappingInfo()
        {
            this.nodeToCodeMap = new Dictionary<ICodeNode, OffsetSpan>();
            this.InstructionToCodeMap = new Dictionary<Instruction, OffsetSpan>();
        }

        // Exposed as internal for testing purposes
        internal Dictionary<Instruction, OffsetSpan> InstructionToCodeMap { get; private set; }

        public OffsetSpan this[ICodeNode node]
        {
            get
            {
                return this.nodeToCodeMap[node];
            }
        }

        public OffsetSpan this[Instruction instruction]
        {
            get
            {
                return this.InstructionToCodeMap[instruction];
            }
        }

        public void Add(ICodeNode node, OffsetSpan span)
        {
            this.nodeToCodeMap.Add(node, span);
        }

        public void Add(Instruction instruction, OffsetSpan span)
        {
            this.InstructionToCodeMap.Add(instruction, span);
        }

        public bool ContainsKey(ICodeNode node)
        {
            return this.nodeToCodeMap.ContainsKey(node);
        }

        public bool ContainsKey(Instruction instruction)
        {
            return this.InstructionToCodeMap.ContainsKey(instruction);
        }
    }
}
