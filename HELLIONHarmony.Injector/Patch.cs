using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Reflection = System.Reflection;

namespace HELLIONHarmony.Injector
{
    public static class Patch
    {
        #region Definition Getting
        public static AssemblyDefinition GetAssemblyDefinition(string path, string[] dependencySearchDirs)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Failed to find file at path " + path);
            }
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();

            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(path));
            foreach (string directory in dependencySearchDirs ?? Array.Empty<string>())
            {
                assemblyResolver.AddSearchDirectory(directory);
            }

            ReaderParameters readerParams = new ReaderParameters { AssemblyResolver = assemblyResolver, ReadWrite = false, InMemory = true }; // TODO invert ReadWrite

            return AssemblyDefinition.ReadAssembly(path, readerParams);
        }

        public static AssemblyDefinition GetAssemblyDefinition(string path) => GetAssemblyDefinition(path, null!);

        public static MethodDefinition GetMethodDefinition(AssemblyDefinition assembly, string fullName, string methodName)
        {
            return assembly.MainModule.GetType(fullName).Methods.First(method => method.Name == methodName);
        }
        public static TypeReference? GetTypeReferenceFromType(AssemblyDefinition assembly, Type type)
        {
            return Utils.TypeDefinitionFor(type, assembly);
        }
        // It is easier to just use the reference assembly
        public static TypeReference? GetSystemTypeReference(Type type)
        {
            return Utils.SystemTypeReference(type);
        }
        #endregion

        #region Patching
        private static List<Instruction> GetInstructionRange(ILProcessor processor, Instruction firstInstruction, Instruction lastInstruction, bool inclusive = true)
        {
            List<Instruction>? instructions = new();
            bool startAdding = false;
            foreach (Instruction instruction in processor.Body.Instructions)
            {
                if (instruction == firstInstruction)
                    startAdding = true;
                if (startAdding)
                    instructions.Add(instruction);
                if (lastInstruction == instruction)
                    break;

            }
            if (!instructions.Contains(firstInstruction))
                throw new ArgumentException("First instruction not part of processor's instructions", nameof(firstInstruction));
            if (!instructions.Contains(lastInstruction))
                throw new ArgumentException("Last instruction not part of processor's instructions", nameof(lastInstruction));

            if (!inclusive)
                instructions.Remove(lastInstruction);

            return instructions;
        }

        public enum PatchMode
        {
            NoInsert = 0,
            Before = 1,
            After = 2,
        }

        private static PatchedInstructionSet CreatePatch(ILProcessor processor, bool shortCircuit, string patchName, PatchMode patchMode = PatchMode.Before)
        {
            // Get any existing patches by the same name
            List<PatchedInstructionSet> sets = PatchedInstructionSet.GetPatchedInstructionSets(processor);
            PatchedInstructionSet? patch = sets.FirstOrDefault(p => p.Name == patchName);
            if (patch == null)
                patch = new PatchedInstructionSet(patchName, shortCircuit);

            patch.RemoveFromProcessor(processor);

            patch.ShortCircuit = shortCircuit;
            patch.ClearInstructions();

            if (patchMode == PatchMode.Before)
                patch.InsertAtStart(processor);
            else if (patchMode == PatchMode.After)
                patch.InsertAtEnd(processor);

            /* Expected shortcircuit output:
             *  ldstr "HELLIONHarmonyPatch|Name|ShortCircuit"
             *  pop
             *  ...
             *  ret
             *  ldrstr "HELLIONHarmonyPatch\"
             *  pop
             *  ...
             *  ret
             */

            return patch;
        }

        public static PatchedInstructionSet MidMethod(AssemblyDefinition assembly, MethodDefinition targetMethod, InstructionSequence targetInstructionSequence, Instruction[]? newInstructions, string patchName, out PatchedInstructionSet skipSet, bool skipPastSequence = false, PatchMode patchMode = PatchMode.Before)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));
            if (targetInstructionSequence == null)
                throw new ArgumentNullException(nameof(targetInstructionSequence));

            ILProcessor processor = targetMethod.Body.GetILProcessor();

            List<Instruction>? sequence = InstructionSequence.FindSequence(processor, targetInstructionSequence);

            if (sequence == null || sequence.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(targetInstructionSequence), "Sequence was not found in target method's instructions");

            int lastSequenceInstructionIndex = processor.Body.Instructions.IndexOf(sequence.Last());

            if (lastSequenceInstructionIndex == -1)
                throw new Exception(nameof(InstructionSequence.FindSequence) + "did not return a valid sequence");

            Instruction afterSequenceInstruction;
            if (lastSequenceInstructionIndex == processor.Body.Instructions.Count)
                afterSequenceInstruction = processor.Body.Instructions.Last();
            else
                afterSequenceInstruction = processor.Body.Instructions[lastSequenceInstructionIndex];

            Instruction firstSequenceInstruction = sequence.First();

            newInstructions ??= Array.Empty<Instruction>();

            PatchedInstructionSet set = CreatePatch(processor, false, patchName, PatchMode.NoInsert); //we don't want to use CreatePatch's insertion at start/end
            set.InstructionBody.AddRange(newInstructions);

            skipSet = CreatePatch(processor, false, patchName + "; skip", PatchMode.NoInsert);
            if (skipPastSequence)
            {
                Instruction jumpInstruction = Instruction.Create(OpCodes.Br, afterSequenceInstruction.Next ?? afterSequenceInstruction);
                skipSet.AddInstruction(jumpInstruction);
                skipSet.InsertBefore(processor, firstSequenceInstruction);
            }

            if (patchMode == PatchMode.Before)
                set.InsertBefore(processor, firstSequenceInstruction);
            else if (patchMode == PatchMode.After)
                set.InsertAfter(processor, afterSequenceInstruction);

            return set;
        }


        #region Existing Method Patching
        public static PatchedInstructionSet Method0(AssemblyDefinition assembly, MethodDefinition targetMethod, MethodReference? newMethod, bool shortCircuit = false, string patchName = "")
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(targetMethod), "Target assembly was null");
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod), "Target method was null");
            if (newMethod == null && shortCircuit == false)
                throw new ArgumentNullException(nameof(newMethod), "New method was null while " + nameof(shortCircuit) + " was false");

            ILProcessor processor = targetMethod.Body.GetILProcessor();
            Instruction newInstruction;

            PatchedInstructionSet set = CreatePatch(processor, shortCircuit, patchName, PatchMode.Before);

            if (newMethod == null && shortCircuit)
                newInstruction = processor.Create(OpCodes.Nop); //newMethod can be null when we use preventOriginal
            else
                newInstruction = processor.Create(OpCodes.Call, newMethod);

            set.InsertInstruction(processor, newInstruction);

            return set;
        }

        // Does not work with calling methods with more than 0 parameters
        public static PatchedInstructionSet Method0(AssemblyDefinition assembly, string @class, string method, Type type, string methodName, bool preventOriginal = false, string? patchName = null)
        {
            TypeDefinition targetType = assembly.MainModule.GetType(@class);
            MethodDefinition targetMethod = targetType.Methods.First(m => m.Name == method);

            patchName ??= type.FullName + "::" + methodName;

            MethodReference newMethod = assembly.MainModule.ImportReference(type.GetMethod(methodName, (Reflection.BindingFlags)(4 | 8 | 16 | 32)));
            return Method0(assembly, targetMethod, newMethod, preventOriginal, methodName);
        }
        // Call a method, passing this instance (or argument 0)
        public static PatchedInstructionSet MethodThis(AssemblyDefinition assembly, MethodDefinition targetMethod, Reflection.MethodInfo newMethod, bool preventOriginal = false, string? patchName = null, PatchMode patchMode = PatchMode.Before, bool passNulls = true)
        {
            if (!passNulls && targetMethod.Parameters.Count > 1)
                throw new ArgumentException("Passed method has too many parameters, cannot pass this instace", nameof(targetMethod));

            patchName ??= (newMethod.DeclaringType?.FullName ?? "?") + "::" + newMethod.Name;

            MethodReference newMethodReference = assembly.MainModule.ImportReference(newMethod);

            ILProcessor processor = targetMethod.Body.GetILProcessor();
            PatchedInstructionSet set = CreatePatch(processor, preventOriginal, patchName, patchMode);

            Instruction callInstruction = Instruction.Create(OpCodes.Call, newMethodReference);

            for (int i = 0; i < targetMethod.Parameters.Count; i++)
            {
                Instruction ldarg = i switch
                {
                    0 => Instruction.Create(OpCodes.Ldarg_0),
                    1 => Instruction.Create(OpCodes.Ldarg_1),
                    2 => Instruction.Create(OpCodes.Ldarg_2),
                    3 => Instruction.Create(OpCodes.Ldarg_3),
                    _ => Instruction.Create(OpCodes.Ldarg_S, i),
                };
                set.AddInstruction(ldarg);
            }
            set.AddInstruction(callInstruction);

            if (patchMode == PatchMode.Before)
                set.InsertAtStart(processor);
            else if (patchMode == PatchMode.After)
                set.InsertAtEnd(processor);

            return set;
        }
        public static PatchedInstructionSet MethodThis(AssemblyDefinition assembly, string @class, string method, Type type, string methodName, bool preventOriginal = false, string? patchName = null)
        {
            TypeDefinition targetType = assembly.MainModule.GetType(@class);
            MethodDefinition targetMethod = targetType.Methods.First(m => m.Name == method);

            Reflection.MethodInfo? newMethod = type.GetMethod(methodName, (Reflection.BindingFlags)(4 | 8 | 16 | 32));
            if (newMethod == null)
                throw new ArgumentException($"Type {type.FullName} did not contain method {methodName}");

            patchName ??= type.FullName + "::" + methodName;

            return MethodThis(assembly, targetMethod, newMethod, preventOriginal, patchName);
        }
        #endregion

        #region Getting existing fields
        public static FieldDefinition? GetField(TypeDefinition type, string fieldName, Type fieldType)
        {
            return type.Fields.FirstOrDefault(m => m.Name == fieldName && m.FieldType.FullName == fieldType.FullName);
        }
        public static FieldDefinition? GetField(AssemblyDefinition assembly, string @class, string fieldName, Type fieldType)
        {
            TypeDefinition targetType = assembly.MainModule.GetType(@class);
            if (targetType == null)
                throw new FileNotFoundException($"Class {@class} did not exist in the assembly");
            return GetField(targetType, fieldName, fieldType);
        }
        #endregion

        #region New Field
        internal static FieldDefinition AddField(AssemblyDefinition assembly, string @class, FieldDefinition fieldDefinition)
        {
            assembly.MainModule.GetType(@class).Fields.Add(fieldDefinition);
            return fieldDefinition;
        }
        public static FieldDefinition NewField(AssemblyDefinition assembly, string @class, string fieldName, Type type, FieldAttributes attributes = FieldAttributes.Public)
        {
            TypeDefinition? typeDefinition = GetTypeReferenceFromType(assembly, type)?.Resolve();
            if (typeDefinition == null)
            {
                typeDefinition = GetSystemTypeReference(type)?.Resolve();
                if (typeDefinition == null)
                    throw new ArgumentException("Type was not found in " + @class, nameof(type));
            }
            return NewField(assembly, @class, fieldName, typeDefinition, attributes);
        }

        public static FieldDefinition NewField(AssemblyDefinition assembly, string @class, string fieldName, TypeDefinition typeDefinition, FieldAttributes attributes = FieldAttributes.Public)
        {
            if (typeDefinition == null)
            {
                throw new ArgumentException("typeDefinition was null", nameof(typeDefinition));
            }
            FieldDefinition fieldDefinition = new FieldDefinition(fieldName, attributes, typeDefinition);
            return AddField(assembly, @class, fieldDefinition);
        }
        public static FieldDefinition NewStaticField(AssemblyDefinition assembly, string @class, string fieldName, TypeDefinition typeDefinition) =>
            NewField(assembly, @class, fieldName, typeDefinition, FieldAttributes.Public | FieldAttributes.Static);
        public static FieldDefinition NewStaticField(AssemblyDefinition assembly, string @class, string fieldName, Type type) =>
            NewField(assembly, @class, fieldName, type, FieldAttributes.Public | FieldAttributes.Static);

        #endregion

        #region New Methods
        public static MethodDefinition NewMethod(AssemblyDefinition assembly, string @class, MethodReference newMethod)
        {
            TypeDefinition typeDefinition = assembly.MainModule.GetType(@class);
            if (typeDefinition == null)
                throw new ArgumentException("Class was not found in assembly", nameof(@class));
            if (newMethod == null || newMethod.Resolve() == null)
                throw new ArgumentException("New method was null", nameof(newMethod));

            MethodDefinition newMethodDefinition = newMethod.Resolve();
            typeDefinition.Methods.Add(newMethodDefinition);
            return newMethodDefinition;
        }
        public static MethodDefinition NewMethod(AssemblyDefinition assembly, string @class, Type methodClass, string methodName) =>
            NewMethod(assembly, @class, assembly.MainModule.ImportReference(methodClass.GetMethod(methodName)));
        #endregion

        #endregion
        public static void SaveAssembly(AssemblyDefinition assembly, string assemblyPath)
        {
            assembly.Write(assemblyPath);
        }
    }
}
