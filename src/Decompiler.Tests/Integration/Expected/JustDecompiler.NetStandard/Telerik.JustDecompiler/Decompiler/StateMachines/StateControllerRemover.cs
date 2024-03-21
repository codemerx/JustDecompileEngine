using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using Telerik.JustDecompiler.Cil;
using Telerik.JustDecompiler.Decompiler;

namespace Telerik.JustDecompiler.Decompiler.StateMachines
{
	internal class StateControllerRemover
	{
		protected readonly MethodSpecificContext methodContext;

		protected readonly ControlFlowGraph theCFG;

		protected readonly InstructionBlock[] stateToStartBlock;

		private FieldDefinition stateField;

		private Telerik.JustDecompiler.Cil.SwitchData switchData;

		private InstructionBlock defaultStateEntry;

		protected VariableReference stateVariable;

		protected int firstControllerBlock;

		protected readonly HashSet<InstructionBlock> toBeRemoved = new HashSet<InstructionBlock>();

		public HashSet<InstructionBlock> BlocksMarkedForRemoval
		{
			get
			{
				return this.toBeRemoved;
			}
		}

		public FieldDefinition StateField
		{
			get
			{
				return this.stateField;
			}
		}

		public Telerik.JustDecompiler.Cil.SwitchData SwitchData
		{
			get
			{
				return this.switchData;
			}
		}

		public StateControllerRemover(MethodSpecificContext methodContext, FieldDefinition stateField = null)
		{
			this.methodContext = methodContext;
			this.theCFG = methodContext.ControlFlowGraph;
			this.stateField = stateField;
			this.stateToStartBlock = new InstructionBlock[(int)this.theCFG.Blocks.Length];
		}

		private bool CheckAndSaveStateField(FieldReference foundStateField)
		{
			FieldDefinition fieldDefinition = foundStateField.Resolve();
			if (this.stateField == null)
			{
				this.stateField = fieldDefinition;
			}
			else if ((object)this.stateField != (object)fieldDefinition)
			{
				return false;
			}
			return true;
		}

		protected bool ContainsStateFieldLoad(InstructionBlock theBlock)
		{
			for (Instruction i = theBlock.First; (object)i != (object)theBlock.Last; i = i.get_Next())
			{
				if (i.get_OpCode().get_Code() == 120 && (object)((FieldReference)i.get_Operand()).Resolve() == (object)this.stateField)
				{
					return true;
				}
			}
			return false;
		}

		private bool ContainsStateVariableSet(InstructionBlock theBlock)
		{
			VariableReference variableReference;
			for (Instruction i = theBlock.First; (object)i != (object)theBlock.Last; i = i.get_Next())
			{
				if (this.IsStloc(i) && this.TryGetVariableFromInstruction(i, out variableReference) && (object)variableReference == (object)this.stateVariable)
				{
					return true;
				}
			}
			return false;
		}

		private void CreateControllerSwitchData()
		{
			int indexOfLastNonNullElement = this.GetIndexOfLastNonNullElement(this.stateToStartBlock);
			int num = indexOfLastNonNullElement + 1;
			indexOfLastNonNullElement = num;
			InstructionBlock[] instructionBlockArrays = new InstructionBlock[num];
			for (int i = 0; i < indexOfLastNonNullElement; i++)
			{
				if (this.stateToStartBlock[i] != null)
				{
					instructionBlockArrays[i] = this.stateToStartBlock[i];
				}
				else
				{
					instructionBlockArrays[i] = this.defaultStateEntry;
				}
			}
			this.switchData = new Telerik.JustDecompiler.Cil.SwitchData(null, this.defaultStateEntry, instructionBlockArrays);
		}

		private int GetIndexOfLastNonNullElement(InstructionBlock[] blocks)
		{
			for (int i = (int)blocks.Length - 1; i >= 0; i--)
			{
				if (blocks[i] != null)
				{
					return i;
				}
			}
			return -1;
		}

		protected bool GetStateFieldAndVariable()
		{
			for (Instruction i = this.theCFG.Blocks[this.firstControllerBlock].First; (object)i != (object)this.theCFG.Blocks[this.firstControllerBlock].Last; i = i.get_Next())
			{
				if (i.get_OpCode().get_Code() == 120)
				{
					if (!this.CheckAndSaveStateField((FieldReference)i.get_Operand()))
					{
						return false;
					}
					this.TryGetVariableFromInstruction(i.get_Next(), out this.stateVariable);
					return true;
				}
			}
			return false;
		}

		protected virtual Queue<InstructionBlock> InitializeTheTraversalQueue()
		{
			Queue<InstructionBlock> instructionBlocks = new Queue<InstructionBlock>();
			instructionBlocks.Enqueue(this.theCFG.Blocks[this.firstControllerBlock]);
			return instructionBlocks;
		}

		protected bool IsBeqInstruction(Instruction theInstruction)
		{
			if (theInstruction.get_OpCode().get_Code() == 58)
			{
				return true;
			}
			return theInstruction.get_OpCode().get_Code() == 45;
		}

		private bool IsBneInstruction(Instruction theInstruction)
		{
			if (theInstruction.get_OpCode().get_Code() == 63)
			{
				return true;
			}
			return theInstruction.get_OpCode().get_Code() == 50;
		}

		private bool IsBrFalseInstruction(Instruction theInstruction)
		{
			if (theInstruction.get_OpCode().get_Code() == 56)
			{
				return true;
			}
			return theInstruction.get_OpCode().get_Code() == 43;
		}

		private bool IsControllerCondition(Instruction instruction)
		{
			if (this.IsBeqInstruction(instruction) || this.IsBneInstruction(instruction))
			{
				return true;
			}
			return instruction.get_OpCode().get_Code() == 68;
		}

		private bool IsDebugControllerBlock(InstructionBlock theBlock, out int stateNumber)
		{
			if ((object)theBlock.First == (object)theBlock.Last)
			{
				stateNumber = -1;
				return false;
			}
			Instruction next = theBlock.First.get_Next();
			if ((object)next == (object)theBlock.Last || next.get_OpCode().get_Code() != 120 || (object)((FieldReference)next.get_Operand()).Resolve() != (object)this.stateField)
			{
				stateNumber = -1;
				return false;
			}
			next = next.get_Next();
			if ((object)next == (object)theBlock.Last || !StateMachineUtilities.TryGetOperandOfLdc(next, out stateNumber))
			{
				stateNumber = -1;
				return false;
			}
			next = next.get_Next();
			if ((object)next == (object)theBlock.Last || next.get_OpCode().get_Code() != 192)
			{
				return false;
			}
			next = next.get_Next();
			if ((object)next == (object)theBlock.Last || next.get_OpCode().get_Code() != 22)
			{
				return false;
			}
			if (theBlock.Last.get_OpCode().get_Code() == 57)
			{
				return true;
			}
			return theBlock.Last.get_OpCode().get_Code() == 44;
		}

		private bool IsStateMachineControllerBlock(ref InstructionBlock theBlock, out StateControllerRemover.StateMachineControllerType controllerType, out int stateNumber)
		{
			VariableReference variableReference;
			theBlock = this.SkipBranchChain(theBlock);
			Instruction last = theBlock.Last;
			if (this.theCFG.SwitchBlocksInformation.ContainsKey(theBlock))
			{
				controllerType = StateControllerRemover.StateMachineControllerType.Switch;
				if (last.get_Previous().get_OpCode().get_Code() == 88 || last.get_Previous().get_OpCode().get_Code() == 184)
				{
					last = last.get_Previous().get_Previous();
					if (!StateMachineUtilities.TryGetOperandOfLdc(last, out stateNumber))
					{
						return false;
					}
				}
				else
				{
					stateNumber = 0;
				}
			}
			else if (this.IsBeqInstruction(theBlock.Last))
			{
				controllerType = StateControllerRemover.StateMachineControllerType.Condition;
				last = theBlock.Last.get_Previous();
				if (!StateMachineUtilities.TryGetOperandOfLdc(last, out stateNumber))
				{
					return false;
				}
			}
			else if (!this.IsBrFalseInstruction(theBlock.Last))
			{
				if (!this.IsBneInstruction(theBlock.Last))
				{
					if (this.IsDebugControllerBlock(theBlock, out stateNumber))
					{
						controllerType = StateControllerRemover.StateMachineControllerType.NegativeCondition;
						return true;
					}
					controllerType = StateControllerRemover.StateMachineControllerType.None;
					stateNumber = 0;
					return false;
				}
				controllerType = StateControllerRemover.StateMachineControllerType.NegativeCondition;
				last = theBlock.Last.get_Previous();
				if (!StateMachineUtilities.TryGetOperandOfLdc(last, out stateNumber))
				{
					return false;
				}
			}
			else
			{
				controllerType = StateControllerRemover.StateMachineControllerType.Condition;
				last = theBlock.Last;
				stateNumber = 0;
			}
			if (this.ContainsStateFieldLoad(theBlock) || this.TryGetVariableFromInstruction(last.get_Previous(), out variableReference) && (object)variableReference == (object)this.stateVariable && !this.ContainsStateVariableSet(theBlock))
			{
				return true;
			}
			stateNumber = 0;
			return false;
		}

		private bool IsStloc(Instruction instruction)
		{
			if (instruction.get_OpCode().get_Code() == 204 || instruction.get_OpCode().get_Code() == 10 || instruction.get_OpCode().get_Code() == 11 || instruction.get_OpCode().get_Code() == 12 || instruction.get_OpCode().get_Code() == 13)
			{
				return true;
			}
			return instruction.get_OpCode().get_Code() == 19;
		}

		protected virtual bool IsUnconditionalBranchBlock(InstructionBlock theBlock)
		{
			return StateMachineUtilities.IsUnconditionalBranch(theBlock.First);
		}

		private void ReattachDefaultSuccessor(InstructionBlock initialBlock, InstructionBlock currentBlock)
		{
			if (initialBlock != currentBlock)
			{
				this.RedirectNonControllerPredecessors(initialBlock, currentBlock);
				this.SwapCFGBlocks(initialBlock.Index, currentBlock.Index);
				for (int i = 0; i < (int)this.stateToStartBlock.Length; i++)
				{
					if (this.stateToStartBlock[i] == initialBlock)
					{
						this.stateToStartBlock[i] = currentBlock;
					}
				}
			}
		}

		private void RedirectNonControllerPredecessors(InstructionBlock controllerBlock, InstructionBlock successor)
		{
			foreach (InstructionBlock instructionBlocks in new List<InstructionBlock>(controllerBlock.Predecessors))
			{
				if (this.toBeRemoved.Contains(instructionBlocks))
				{
					continue;
				}
				successor.Predecessors.Remove(controllerBlock);
				this.RedirectSuccessor(instructionBlocks, controllerBlock, successor);
			}
		}

		private void RedirectSuccessor(InstructionBlock theBlock, InstructionBlock oldSuccessor, InstructionBlock newSuccessor)
		{
			Telerik.JustDecompiler.Cil.SwitchData switchDatum;
			oldSuccessor.Predecessors.Remove(theBlock);
			for (int i = 0; i < (int)theBlock.Successors.Length; i++)
			{
				if (theBlock.Successors[i] == oldSuccessor)
				{
					theBlock.Successors[i] = newSuccessor;
				}
			}
			newSuccessor.Predecessors.Add(theBlock);
			if (!this.theCFG.SwitchBlocksInformation.TryGetValue(theBlock, out switchDatum))
			{
				return;
			}
			for (int j = 0; j < (int)switchDatum.OrderedCasesArray.Length; j++)
			{
				if (switchDatum.OrderedCasesArray[j] == oldSuccessor)
				{
					switchDatum.OrderedCasesArray[j] = newSuccessor;
				}
			}
			if (switchDatum.DefaultCase == oldSuccessor)
			{
				switchDatum.DefaultCase = newSuccessor;
			}
		}

		protected bool RemoveControllerChain()
		{
			InstructionBlock i;
			int num;
			StateControllerRemover.StateMachineControllerType stateMachineControllerType;
			InstructionBlock instructionBlocks;
			StateControllerRemover.ControllerTraversalSearchResult controllerTraversalSearchResult;
			InstructionBlock instructionBlocks1;
			InstructionBlock instructionBlocks2;
			Queue<InstructionBlock> instructionBlocks3 = this.InitializeTheTraversalQueue();
			while (instructionBlocks3.Count > 0)
			{
				InstructionBlock instructionBlocks4 = instructionBlocks3.Dequeue();
				for (i = instructionBlocks4; this.IsStateMachineControllerBlock(ref i, out stateMachineControllerType, out num); i = this.SkipBranchChain(i))
				{
					switch (stateMachineControllerType)
					{
						case StateControllerRemover.StateMachineControllerType.Switch:
						{
							Telerik.JustDecompiler.Cil.SwitchData item = this.theCFG.SwitchBlocksInformation[i];
							InstructionBlock[] orderedCasesArray = item.OrderedCasesArray;
							for (int j = 0; j < (int)orderedCasesArray.Length; j++)
							{
								if (!this.toBeRemoved.Contains(orderedCasesArray[j]))
								{
									controllerTraversalSearchResult = this.TryGetStateEntry(orderedCasesArray[j], out instructionBlocks);
									switch (controllerTraversalSearchResult)
									{
										case StateControllerRemover.ControllerTraversalSearchResult.PatternFailed:
										{
											return false;
										}
										case StateControllerRemover.ControllerTraversalSearchResult.FoundStateEntry:
										{
											this.stateToStartBlock[j + num] = instructionBlocks;
											break;
										}
										case StateControllerRemover.ControllerTraversalSearchResult.FoundControllerCandidate:
										{
											this.stateToStartBlock[j + num] = instructionBlocks;
											instructionBlocks3.Enqueue(instructionBlocks);
											break;
										}
									}
								}
							}
							instructionBlocks3.Enqueue(this.SkipBranchChain(item.DefaultCase));
							break;
						}
						case StateControllerRemover.StateMachineControllerType.Condition:
						{
							controllerTraversalSearchResult = this.TryGetStateEntry(i.Successors[0], out instructionBlocks1);
							switch (controllerTraversalSearchResult)
							{
								case StateControllerRemover.ControllerTraversalSearchResult.PatternFailed:
								{
									return false;
								}
								case StateControllerRemover.ControllerTraversalSearchResult.FoundStateEntry:
								{
									this.stateToStartBlock[num] = instructionBlocks1;
									break;
								}
								case StateControllerRemover.ControllerTraversalSearchResult.FoundControllerCandidate:
								{
									this.stateToStartBlock[num] = instructionBlocks1;
									instructionBlocks3.Enqueue(instructionBlocks1);
									break;
								}
							}
							break;
						}
						case StateControllerRemover.StateMachineControllerType.NegativeCondition:
						{
							int num1 = 1;
							if (num == -1)
							{
								num = 0;
								num1 = 0;
							}
							controllerTraversalSearchResult = this.TryGetStateEntry(i.Successors[num1], out instructionBlocks2);
							if (controllerTraversalSearchResult == StateControllerRemover.ControllerTraversalSearchResult.PatternFailed)
							{
								return false;
							}
							if ((int)controllerTraversalSearchResult - (int)StateControllerRemover.ControllerTraversalSearchResult.FoundStateEntry > (int)StateControllerRemover.ControllerTraversalSearchResult.FoundStateEntry)
							{
								break;
							}
							this.stateToStartBlock[num] = instructionBlocks2;
							break;
						}
					}
					this.toBeRemoved.Add(i);
					i = (stateMachineControllerType != StateControllerRemover.StateMachineControllerType.NegativeCondition ? i.Successors[(int)i.Successors.Length - 1] : i.Successors[0]);
				}
				if (this.defaultStateEntry == null)
				{
					this.defaultStateEntry = i;
				}
				this.ReattachDefaultSuccessor(instructionBlocks4, i);
				while (instructionBlocks3.Count > 0 && this.toBeRemoved.Contains(instructionBlocks3.Peek()))
				{
					instructionBlocks3.Dequeue();
				}
			}
			if (this.toBeRemoved.Count == 0)
			{
				return false;
			}
			this.CreateControllerSwitchData();
			return true;
		}

		public virtual bool RemoveStateMachineController()
		{
			if (!this.GetStateFieldAndVariable())
			{
				return false;
			}
			return this.RemoveControllerChain();
		}

		protected InstructionBlock SkipBranchChain(InstructionBlock initialBlock)
		{
			InstructionBlock successors = initialBlock;
			while (this.IsUnconditionalBranchBlock(successors))
			{
				successors = successors.Successors[0];
			}
			return successors;
		}

		private void SwapCFGBlocks(int firstIndex, int secondIndex)
		{
			InstructionBlock blocks = this.theCFG.Blocks[firstIndex];
			InstructionBlock instructionBlocks = this.theCFG.Blocks[secondIndex];
			this.theCFG.Blocks[firstIndex] = instructionBlocks;
			this.theCFG.Blocks[secondIndex] = blocks;
			blocks.Index = secondIndex;
			instructionBlocks.Index = firstIndex;
		}

		protected virtual StateControllerRemover.ControllerTraversalSearchResult TryGetStateEntry(InstructionBlock theBlock, out InstructionBlock actualSuccessor)
		{
			actualSuccessor = this.SkipBranchChain(theBlock);
			if (this.IsControllerCondition(actualSuccessor.Last))
			{
				return StateControllerRemover.ControllerTraversalSearchResult.FoundControllerCandidate;
			}
			return StateControllerRemover.ControllerTraversalSearchResult.FoundStateEntry;
		}

		protected bool TryGetVariableFromInstruction(Instruction instruction, out VariableReference varReference)
		{
			return StateMachineUtilities.TryGetVariableFromInstruction(instruction, this.methodContext.Body.get_Variables(), out varReference);
		}

		protected enum ControllerTraversalSearchResult
		{
			PatternFailed,
			FoundStateEntry,
			FoundControllerCandidate
		}

		private enum StateMachineControllerType
		{
			None,
			Switch,
			Condition,
			NegativeCondition
		}
	}
}