using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MCG = Mono.Collections.Generic;

namespace HELLIONHarmony.Injector
{
    using static CecilInstructionExtensions;

    public class InstructionSequence
    {
        public List<Instruction> instructions;

        public InstructionSequence()
        {
            instructions = new List<Instruction>();
        }
        public InstructionSequence(List<Instruction> instructions)
        {
            this.instructions = instructions;
        }

        public void PrintSequence()
        {
            foreach (Instruction instruction in instructions)
            {
                if (instruction == null)
                    Console.WriteLine("#any:#any");
                else
                    Console.WriteLine(instruction.OpCode + ":" + instruction.Operand);
            }
        }

        #region Fluent interface statements
        public InstructionSequence AddInstruction(Instruction instruction)
        {
            instructions.Add(instruction);
            return this;
        }
        public InstructionSequence AddInstruction(OpCode opCode, dynamic value)
        {
            instructions.Add(Instruction.Create(opCode, value));
            return this;
        }
        public InstructionSequence AddAnyOpCode()
        {
            instructions.Add(null);
            return this;
        }
        public InstructionSequence AddAnyOperand(OpCode opCode)
        {
            instructions.Add(AmbiguousInstructionAny(opCode));
            return this;
        }
        public InstructionSequence AddOperandIsNull(OpCode opCode)
        {
            instructions.Add(AmbiguousInstructionIsNull(opCode));
            return this;
        }
        public InstructionSequence AddOperandNotNull(OpCode opCode)
        {
            instructions.Add(AmbiguousInstructionNotNull(opCode));
            return this;
        }
        public InstructionSequence AddOperandOfType(OpCode opCode, Type type)
        {
            instructions.Add(AmbiguousInstructionOfType(opCode, type));
            return this;
        }
        public InstructionSequence AddOperandContains(OpCode opCode, string containString)
        {
            instructions.Add(AmbiguousInstructionContains(opCode, containString));
            return this;
        }
        public InstructionSequence AddOperandStartsWith(OpCode opCode, string startString)
        {
            instructions.Add(AmbiguousInstructionStartsWith(opCode, startString));
            return this;
        }
        public InstructionSequence AddOperandEndsWith(OpCode opCode, string startString)
        {
            instructions.Add(AmbiguousInstructionEndsWith(opCode, startString));
            return this;
        }
        public InstructionSequence AddOperandIndexOf(OpCode opCode, string startString, int index)
        {
            instructions.Add(AmbiguousInstructionIndexOf(opCode, startString, index));
            return this;
        }
        public InstructionSequence AddOperandLastIndexOf(OpCode opCode, string startString, int index)
        {
            instructions.Add(AmbiguousInstructionLastIndexOf(opCode, startString, index));
            return this;
        }
        #endregion

        // TODO check works
        public static List<Instruction>? FindSequence(IList<Instruction> processorInstructions, IList<Instruction> sequenceInstructions)
        {
            int sequenceLength = sequenceInstructions.Count();
            int processorInstructionCount = processorInstructions.Count();
            List<Instruction> matchingInstructions = new List<Instruction>();
            for (int p = 0; p < processorInstructionCount; p++) // go over every 
            {
                int pi = p;
                int matchCount = 0;
                matchingInstructions.Clear();

                for (int i = 0; i < sequenceLength; i++) // iterate over the find instructions to see if we have a match
                {
                    Instruction sequenceInstruction = sequenceInstructions[i];
                    if (processorInstructionCount - pi < sequenceLength || pi > processorInstructionCount) // we have run out of instructions to search through
                        break;
                    Instruction instruction = processorInstructions[pi];
                    if (sequenceInstruction == null || instruction.MatchesAmbigious(sequenceInstruction, false)) // we have a match for this part of the sequence
                    {
                        matchCount++;
                        pi++;
                        matchingInstructions.Add(instruction);
                        continue;
                    }
                    break; // there has been no match
                }
                if (matchCount == sequenceLength) // if the number of matching instructions equals the amount we need to find, all matching in sequence then we have found our sequence
                {
                    return matchingInstructions;
                }
            }
            return null; // we didn't find the sequence in the processor instructions
        }
        public static List<Instruction>? FindSequence(ILProcessor processor, IList<Instruction> findSequence) => FindSequence(processor.Body.Instructions, findSequence);
        public static List<Instruction>? FindSequence(ILProcessor processor, InstructionSequence sequence) => FindSequence(processor.Body.Instructions, sequence.instructions);
        public static List<Instruction>? FindSequence(IList<Instruction> processorInstructions, InstructionSequence sequence) => FindSequence(processorInstructions, sequence.instructions);
        public List<Instruction>? FindSequence(ILProcessor processor) => FindSequence(processor.Body.Instructions, this);
        public List<Instruction>? FindSequence(IList<Instruction> processorInstructions) => FindSequence(processorInstructions, this);
        public static List<Instruction>? FindSequenceInMethod(MethodDefinition method, InstructionSequence sequence) => FindSequence(method.Body.Instructions, sequence);
        public static List<Instruction>? FindSequenceInMethod(MethodReference method, InstructionSequence sequence) => FindSequence(method.Resolve().Body.Instructions, sequence);
        public List<Instruction>? FindSequenceInMethod(MethodDefinition method) => FindSequence(method.Body.Instructions, this);
        public List<Instruction>? FindSequenceInMethod(MethodReference method) => FindSequence(method.Resolve().Body.Instructions, this);
    }
}
