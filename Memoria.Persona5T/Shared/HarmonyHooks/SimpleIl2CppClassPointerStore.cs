using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;

namespace Memoria.Persona5T.HarmonyHooks;

public static class SimpleIl2CppClassPointerStore
{
    public static Dictionary<Type, IntPtr> _classPointers = new Dictionary<Type, IntPtr>();

    public static IntPtr NativeClassPtr;
    public static Type CreatedTypeRedirect;

    public static IntPtr GetNativeClassPtr(Type type)
    {
        if (_classPointers.TryGetValue(type, out var result))
            return result;

        if (!type.IsEnum)
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }
        else
        {
            String name1 = type.Module.Name;
            String namespaze = type.Namespace ?? "";
            String name2 = type.Name;
            foreach (CustomAttributeData customAttribute in type.CustomAttributes)
            {
                if (!(customAttribute.AttributeType != typeof(OriginalNameAttribute)))
                {
                    CustomAttributeTypedArgument constructorArgument = customAttribute.ConstructorArguments[0];
                    name1 = (String)constructorArgument.Value;
                    constructorArgument = customAttribute.ConstructorArguments[1];
                    namespaze = (String)constructorArgument.Value;
                    constructorArgument = customAttribute.ConstructorArguments[2];
                    name2 = (String)constructorArgument.Value;
                }
            }

            result = !type.IsNested ? Il2CppInterop.Runtime.IL2CPP.GetIl2CppClass(name1, namespaze, name2) : Il2CppInterop.Runtime.IL2CPP.GetIl2CppNestedType(Il2CppClassPointerStore.GetNativeClassPointer(type.DeclaringType), name2);
            _classPointers.Add(type, result);
        }

        if (type.IsPrimitive || type == typeof(String))
            RuntimeHelpers.RunClassConstructor(((IEnumerable<Assembly>)AppDomain.CurrentDomain.GetAssemblies()).Single<Assembly>((Func<Assembly, Boolean>)(it => it.GetName().Name == "Il2Cppmscorlib")).GetType("Il2Cpp" + type.FullName).TypeHandle);
        foreach (CustomAttributeData customAttribute in type.CustomAttributes)
        {
            if (!(customAttribute.AttributeType != typeof(AlsoInitializeAttribute)))
                RuntimeHelpers.RunClassConstructor(((Type)customAttribute.ConstructorArguments[0].Value).TypeHandle);
        }

        return result;
    }
}