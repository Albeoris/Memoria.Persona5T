using System;
using System.Linq;
using System.Reflection;

namespace Memoria.Persona5T.HarmonyHooks;

public static class ReflectionMethods
{
    public static PropertyInfo FindSinglePropertyByName(this Type type, String name, StringComparison stringComparison)
    {
        return type.GetProperties().SingleOrDefault(p => p.Name.Equals(name, stringComparison));
    }
    
    public static PropertyInfo[] GetPropertiesOfType<T>(this Type type)
    {
        Type parentType = typeof(T);
        return type.GetProperties().Where(p => parentType.IsAssignableFrom(p.PropertyType)).ToArray();
    }

    // public static Object CastTo<T>(this T instance, Type targetType) where T : Il2CppObjectBase
    // {
    //     Object obj = instance.TryCastTo(targetType);
    //     if (obj != null)
    //         return obj;
    //
    //     throw new InvalidCastException();
    // }
    //
    // public static Object TryCastTo<T>(this T instance, Type targetType) where T : Il2CppObjectBase
    // {
    //     IntPtr nativeClassPtr = SimpleIl2CppClassPointerStore.GetNativeClassPtr(targetType);
    //     //IntPtr nativeClassPtr = Il2CppClassPointerStore<T>.NativeClassPtr;
    //     if (nativeClassPtr == IntPtr.Zero)
    //         throw new ArgumentException($"Target type: {targetType.FullName}");
    //
    //     IntPtr num = Il2CppInterop.Runtime.IL2CPP.il2cpp_object_get_class(instance.Pointer);
    //     if (!Il2CppInterop.Runtime.IL2CPP.il2cpp_class_is_assignable_from(nativeClassPtr, num))
    //         return default;
    //
    //     if (RuntimeSpecificsStore.IsInjected(num) && ClassInjectorBase.GetMonoObjectFromIl2CppPointer(instance.Pointer) is T fromIl2CppPointer)
    //         return fromIl2CppPointer;
    //
    //     Object initializer = typeof(Il2CppObjectBase.InitializerStore<>).MakeGenericType(targetType).GetProperty("Initializer").GetValue(obj: null);
    //     return initializer.GetType().GetMethod("Invoke").Invoke( initializer, new Object[] { instance.Pointer });
    // }
}