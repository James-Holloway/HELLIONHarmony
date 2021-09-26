using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Reflection = System.Reflection;

namespace HELLIONHarmony.Injector
{
    public static class CecilInstructionExtensions
    {
        /// <summary>
        /// Allows checking
        /// </summary>
        /// <param name="instruction"></param>
        /// <param name="otherInstruction"></param>
        /// <param name="exactMatch"></param>
        /// <returns></returns>
        public static bool AmbigiousMatch(this Instruction instruction, Instruction otherInstruction, bool sameOpCode = true)
        {
            if (instruction == null)
                throw new ArgumentNullException(nameof(instruction));
            if (otherInstruction == null)
                throw new ArgumentNullException(nameof(otherInstruction));

            if (instruction == otherInstruction)
                return true;

            if (sameOpCode && instruction.OpCode == otherInstruction.OpCode)
                return true;

            if (instruction.Operand == otherInstruction.Operand)
                return true;

            if (instruction.Operand is string && AmbigiousOperandMatchLogic((string)instruction.Operand, otherInstruction.Operand))
                return true;
            if (otherInstruction.Operand is string && AmbigiousOperandMatchLogic((string)otherInstruction.Operand, instruction.Operand))
                return true;
            return false;
        }
        /// <summary>
        /// Same as AmbigiousMatch except only the passed instruction be ambigous (to protect against accidental/crafted triggers)
        /// </summary>
        /// <param name="instruction"></param>
        /// <param name="otherInstruction">Ambigious instruction</param>
        /// <param name="sameOpCode">Requires that the OpCodes have to be same, otherwise only the Operand is checked</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool MatchesAmbigious(this Instruction instruction, Instruction otherInstruction, bool sameOpCode = true)
        {
            if (instruction == null)
                throw new ArgumentNullException(nameof(instruction));
            if (otherInstruction == null)
                throw new ArgumentNullException(nameof(otherInstruction));

            if (instruction == otherInstruction)
                return true;

            if (sameOpCode && instruction.OpCode == otherInstruction.OpCode)
                return true;

            if (instruction.Operand == otherInstruction.Operand)
                return true;
            if (instruction.Operand?.GetType() == otherInstruction.Operand?.GetType())
            {
                //instruction.Operand.Equals(otherInstruction.Operand.GetType());
                if (instruction.Operand is string && (string)instruction.Operand == (string)otherInstruction.Operand)
                return true;
            }

            if (otherInstruction.Operand is string && AmbigiousOperandMatchLogic((string)otherInstruction.Operand, instruction.Operand?.ToString() ?? "null"))
                return true;
            return false;
        }

        /// <summary>
        /// Not meant to be used directly but made public just in case
        /// </summary>
        /// <param name="operand"></param>
        /// <param name="otherOperand">Operand to be checked against</param>
        /// <returns></returns>
        public static bool AmbigiousOperandMatchLogic(string operand, object otherOperand)
        {
            if (!operand.StartsWith('#'))
                return false;

            List<string> operandInfo = operand.Substring(1).Split("#").ToList();
            if (operandInfo.Count == 0)
                return false;

            string? action = operandInfo.ElementAtOrDefault(0);
            string? arg1 = operandInfo.ElementAtOrDefault(1);
            string? arg2 = operandInfo.ElementAtOrDefault(2);
            if (action == null)
                return true;
            if (action == "any")
                return true;
            else if (action == "isnull")
                return otherOperand == null;
            else if (action == "notnull")
                return otherOperand != null;
            else if (action == "type")
            {
                Type otherType = otherOperand.GetType();
                return otherType.FullName == arg1;
            }
            else if (otherOperand is string && arg1 != null)
            {
                if (action == "contains")
                    return ((string)otherOperand).Contains(arg1);
                else if (action == "startswith")
                    return ((string)otherOperand).StartsWith(arg1);
                else if (action == "endswith")
                    return ((string)otherOperand).EndsWith(arg1);
                else
                {
                    int.TryParse(arg2, out int index);
                    if (action == "indexof")
                        return ((string)otherOperand).IndexOf(arg1) == index;
                    if (action == "lastindexof")
                        return ((string)otherOperand).LastIndexOf(arg1) == index;
                }
            }

            return true;
        }

        private static readonly Reflection.ConstructorInfo InstructionConstructor = typeof(Instruction).GetConstructor((Reflection.BindingFlags)(4|8|16|32), new Type[] { typeof(OpCode), typeof(object) })!;
        private static Instruction CreateInstructionSetOperand(OpCode opCode, dynamic operand)
        {
            return (Instruction) InstructionConstructor.Invoke(new object[] { opCode, operand });
        }

        public static Instruction AmbiguousInstructionAny(OpCode opCode) => CreateInstructionSetOperand(opCode, "#any");
        public static Instruction AmbiguousInstructionIsNull(OpCode opCode) => CreateInstructionSetOperand(opCode, "#isnull");
        public static Instruction AmbiguousInstructionNotNull(OpCode opCode) => CreateInstructionSetOperand(opCode, "#notnull");
        public static Instruction AmbiguousInstructionOfType(OpCode opCode, Type type) => CreateInstructionSetOperand(opCode, "#type#" + type.FullName);
        public static Instruction AmbiguousInstructionContains(OpCode opCode, string containString) => CreateInstructionSetOperand(opCode, "#contains#" + containString);
        public static Instruction AmbiguousInstructionStartsWith(OpCode opCode, string startString) => CreateInstructionSetOperand(opCode, "#startswith#" + startString);
        public static Instruction AmbiguousInstructionEndsWith(OpCode opCode, string endString) => CreateInstructionSetOperand(opCode, "#endswith#" + endString);
        public static Instruction AmbiguousInstructionIndexOf(OpCode opCode, string matchString, int index) => CreateInstructionSetOperand(opCode, $"#indexof#{matchString}#{index}");
        public static Instruction AmbiguousInstructionLastIndexOf(OpCode opCode, string matchString, int index) => CreateInstructionSetOperand(opCode, $"#lastindexof#{matchString}#{index}");
    }
}
