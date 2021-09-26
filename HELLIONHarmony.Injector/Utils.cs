using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Reflection = System.Reflection;
using Mono.Cecil;

namespace HELLIONHarmony.Injector
{
    internal static class Utils
    {
		// Taken from https://github.com/jbevain/cecil/blob/master/Test/Mono.Cecil.Tests/TypeDefinitionUtils.cs - MIT
		public static TypeDefinition? TypeDefinitionFor(Type type, AssemblyDefinition assemblyDefinition)
		{
			var stack = new Stack<string>();
			var currentType = type;
			while (currentType != null)
			{
				stack.Push((currentType.DeclaringType == null ? currentType.Namespace + "." : "") + currentType.Name);
				currentType = currentType.DeclaringType;
			}

			var typeDefinition = assemblyDefinition.MainModule.GetType(stack.Pop());
			if (typeDefinition == null)
				return null;

			while (stack.Count > 0)
			{
				var name = stack.Pop();
				typeDefinition = typeDefinition.NestedTypes.Single(t => t.Name == name);
			}

			return typeDefinition;
		}

		internal static AssemblyDefinition ExecutingAssembly { get; } = AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location);
		internal static AssemblyDefinition MsCorLib { get; } = ExecutingAssembly.MainModule.TypeSystem.Object.Resolve().Module.Assembly;
		public static TypeReference? SystemTypeReference(Type type)
        {
			return TypeDefinitionFor(type, MsCorLib);
		}

        // Not recommended but it can be handy
        public static MethodReference? MethodReferenceFromMethodInfo(Reflection.MethodInfo desiredMethod)
        {
            if (desiredMethod == null)
                throw new ArgumentNullException(nameof(desiredMethod));

            Type? declaringType = desiredMethod.DeclaringType;
            if (declaringType == null)
                return null;

            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(declaringType.Assembly.Location);
            if (assembly == null)
                throw new FileNotFoundException("Assembly could not be found at the location " + declaringType.Assembly.Location);
            TypeReference? typeReference = TypeDefinitionFor(declaringType, assembly);
            if (typeReference == null)
                return null;

            List<Reflection.ParameterInfo> desiredParameters = desiredMethod.GetParameters().ToList();

            IEnumerable<MethodReference> matchingNameMethods = typeReference.Resolve().Methods.Where(method => method.Name == desiredMethod.Name);
            foreach (MethodReference method in matchingNameMethods)
            {
                List<ParameterDefinition> methodParameters = method.Parameters.ToList();
                if (desiredParameters.Count() == methodParameters.Count())
                {
                    for (int p = 0; p < methodParameters.Count(); p++)
                    {
                        if (methodParameters[p].ParameterType.FullName != desiredParameters[p].ParameterType.FullName)
                            continue;
                    }
                    // all parameters 
                    return method;
                }
            }
            return null;
        }
    }
}
