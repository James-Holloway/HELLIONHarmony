using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace HELLIONHarmony
{
    public class HELLIONPatchAttribute : HarmonyPatch
    {
        PatchScope PatchScope { get; set; }
        public HELLIONPatchAttribute(PatchScope patchScope) : base()
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, Type declaringType) : base(declaringType)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, string methodName) : base(methodName)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, MethodType methodType) : base(methodType)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, Type[] argumentTypes) : base(argumentTypes)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, Type declaringType, Type[] argumentTypes) : base(declaringType, argumentTypes)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, Type declaringType, string methodName) : base(declaringType, methodName)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, Type declaringType, MethodType methodType) : base(declaringType, methodType)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, string methodName, params Type[] argumentTypes) : base(methodName, argumentTypes)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, string methodName, MethodType methodType) : base(methodName, methodType)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, MethodType methodType, params Type[] argumentTypes) : base(methodType, argumentTypes)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, Type[] argumentTypes, ArgumentType[] argumentVariations) : base(argumentTypes, argumentVariations)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, Type declaringType, string methodName, params Type[] argumentTypes) : base(declaringType, methodName, argumentTypes)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, Type declaringType, MethodType methodType, params Type[] argumentTypes) : base(declaringType, methodType, argumentTypes)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, Type declaringType, string methodName, MethodType methodType) : base(declaringType, methodName, methodType)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, string methodName, Type[] argumentTypes, ArgumentType[] argumentVariations) : base(methodName, argumentTypes, argumentVariations)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, MethodType methodType, Type[] argumentTypes, ArgumentType[] argumentVariations) : base(methodType, argumentTypes, argumentVariations)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, Type declaringType, string methodName, Type[] argumentTypes, ArgumentType[] argumentVariations) : base(declaringType, methodName, argumentTypes, argumentVariations)
        {
            PatchScope = patchScope;
        }

        public HELLIONPatchAttribute(PatchScope patchScope, Type declaringType, MethodType methodType, Type[] argumentTypes, ArgumentType[] argumentVariations) : base(declaringType, methodType, argumentTypes, argumentVariations)
        {
            PatchScope = patchScope;
        }
    }
}
