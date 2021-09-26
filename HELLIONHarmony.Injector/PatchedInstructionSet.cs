using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HELLIONHarmony.Injector
{
    public class PatchedInstructionSet
    {
        public readonly static string Identifier = "HELLIONHarmonyPatch";
        public readonly static string IdentifierSeperator = "|";
        public readonly static string IdentifierEnd = "End";

        public string Name; // Should be unique
        public bool ShortCircuit = false;
        public Instruction? StartIdentifier { get; protected set; }
        public Instruction? EndIdentifier { get; protected set; }
        public List<Instruction> InstructionBody;

        public PatchedInstructionSet(string name, bool shortCircuit, IEnumerable<Instruction>? instructions = null)
        {
            Name = name.Replace(IdentifierSeperator, "_"); //.Replace(IdentifierEnd, "_");

            ShortCircuit = shortCircuit;
            InstructionBody = instructions?.ToList() ?? new();
        }

        public List<Instruction> GetInstructions()
        {
            List<Instruction> instructions = new();

            StartIdentifier ??= Instruction.Create(OpCodes.Ldstr, Identifier);
            StartIdentifier.Operand = GetIdentifier();
            Instruction StartIdentifierPop = StartIdentifier.Next ?? Instruction.Create(OpCodes.Pop);

            instructions.Add(StartIdentifier);
            instructions.Add(StartIdentifierPop);

            instructions.AddRange(InstructionBody);

            if (ShortCircuit && instructions.Last().OpCode != OpCodes.Ret)
            {
                instructions.Add(Instruction.Create(OpCodes.Ret));
            }

            EndIdentifier ??= Instruction.Create(OpCodes.Ldstr, Identifier + IdentifierEnd);
            EndIdentifier.Operand = GetEndIdentifier();
            Instruction EndIdentifierPop = EndIdentifier.Next ?? Instruction.Create(OpCodes.Pop);

            instructions.Add(EndIdentifier);
            instructions.Add(EndIdentifierPop);

            return instructions;
        }

        public void InsertAfter(ILProcessor processor, Instruction afterInstruction)
        {
            IncreaseStackSize(processor);
            Instruction previousInstruction = afterInstruction;
            foreach (Instruction instruction in GetInstructions())
            {
                processor.InsertAfter(previousInstruction, instruction);
                previousInstruction = instruction;
            }
        }

        public void InsertBefore(ILProcessor processor, Instruction beforeInstruction)
        {
            IncreaseStackSize(processor);
            Instruction previousInstruction = beforeInstruction; // relatively actually the next instruction, we just do it in reverse
            foreach (Instruction instruction in Enumerable.Reverse(GetInstructions()))
            {
                processor.InsertBefore(previousInstruction, instruction);
                previousInstruction = instruction;
            }
        }

        public void InsertAtStart(ILProcessor processor) => InsertBefore(processor, processor.Body.Instructions.First());
        public void InsertAtEnd(ILProcessor processor) => InsertBefore(processor, processor.Body.Instructions.Last());

        public void RemoveFromProcessor(ILProcessor processor)
        {
            foreach (Instruction instruction in GetInstructions())
            {
                try
                {
                    processor.Remove(instruction);
                }
                catch (ArgumentOutOfRangeException) { }
            }
        }

        public void IncreaseStackSize(ILProcessor processor, int requiredStackSize = 1)
        {
            if (processor.Body.MaxStackSize < requiredStackSize)
                processor.Body.MaxStackSize = requiredStackSize;
        }

        public void ReduceStackSize(ILProcessor processor, int requiredStackSize = 0)
        {
            if (processor.Body.MaxStackSize > requiredStackSize)
                processor.Body.MaxStackSize = requiredStackSize;
        }

        public void ClearInstructions()
        {
            InstructionBody?.Clear();
        }

        public string GetIdentifier() => string.Join(IdentifierSeperator, new[] { Identifier, Name ?? "", ShortCircuit ? "ShortCircuit" : "" });

        public static string GetEndIdentifier() => Identifier + IdentifierEnd;
        public static PatchedInstructionSet? CreateFromIdentifier(string identifier)
        {
            List<string> elements = new(identifier.Split(IdentifierSeperator));
            if (elements.Count == 0)
                return null;
            if (elements.Count == 1 && elements[0] != Identifier)
                return null;

            string name = elements.ElementAtOrDefault(1) ?? "";
            bool shortCircuit = elements.ElementAtOrDefault(2) == "ShortCircuit";
            return new PatchedInstructionSet(name, shortCircuit, null);
        }

        public static List<PatchedInstructionSet> GetPatchedInstructionSets(ILProcessor processor)
        {
            List<PatchedInstructionSet> list = new(1); // We'll take the performance hit, it is most likely there is only one set per patched function
            Stack<PatchedInstructionSet> currentStack = new(1);
            foreach (Instruction instruction in processor.Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Ldstr && instruction.Operand != null)
                {
                    string operand = (string)instruction.Operand;
                    if (operand.StartsWith(Identifier))
                    {
                        if (operand.StartsWith(GetEndIdentifier()))
                        {
                            currentStack.Peek().EndIdentifier = instruction;
                            list.Add(currentStack.Pop());
                        }
                        else // operand is start of patch
                        {
                            PatchedInstructionSet set = CreateFromIdentifier(operand)!;
                            if (set != null)
                            {
                                set.StartIdentifier = instruction;
                                currentStack.Push(set);
                            }
                        }
                    }
                }
                foreach (PatchedInstructionSet instructionSet in currentStack.ToArray())
                {
                    if (currentStack.Peek().StartIdentifier == instruction) // don't add the StartIdentifier to the body
                        continue;
                    if (currentStack.Peek().StartIdentifier == instruction.Previous) //if we are adding the pop of ldstr, don't add it to the body
                        continue;
                    instructionSet?.InstructionBody.Add(instruction); // add the instruction to every instruction body in the stack
                }
            }
            return list;
        }

        public void InsertInstruction(ILProcessor processor, Instruction instruction)
        {
            if (StartIdentifier?.Next == null)
                throw new NullReferenceException("Instructions have not been generated - use GetInstructions()");
            processor.InsertAfter(StartIdentifier.Next, instruction);
        }
        public void InsertInstructions(ILProcessor processor, IEnumerable<Instruction> instructions)
        {
            if (StartIdentifier?.Next == null)
                throw new NullReferenceException("Instructions have not been generated - use GetInstructions()");

            Instruction lastIntruction = StartIdentifier.Next;
            foreach (Instruction instruction in instructions)
            {
                processor.InsertAfter(instruction, lastIntruction);
                lastIntruction = instruction;
            }
        }

        public void AddInstruction(Instruction instruction)
        {
            InstructionBody.Add(instruction);
        }
        public void AddInstructions(IEnumerable<Instruction> instructions)
        {
            InstructionBody.AddRange(instructions);
        }
    }
}
