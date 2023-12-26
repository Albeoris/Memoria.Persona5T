using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonoMod.Utils;

namespace Memoria.Persona5T.HarmonyHooks;

public static class EnumerableWrapper
{
    private static readonly Dictionary<Type, EnumerableProvider> _enumerableProviders = new();

    public static IEnumerable Enumerate(this Object instance)
    {
        Type instanceType = instance.GetType();
     
        if (!_enumerableProviders.TryGetValue(instanceType, out EnumerableProvider value))
        {
            value = new EnumerableProvider(instanceType);
            _enumerableProviders.Add(instanceType, value);
        }

        return value.EnumerateInternal(instance);
    }

    private sealed class EnumerableProvider
    {
        private readonly FastReflectionDelegate _getEnumerator;
        private FastReflectionDelegate _moveNext;
        private FastReflectionDelegate _current;

        public EnumerableProvider(Type instanceType)
        {
            _getEnumerator = AccessTools.FirstMethod(instanceType, m => m.Name == "GetEnumerator").CreateFastDelegate();
        }

        public IEnumerable EnumerateInternal(Object instance)
        {
            Object enumerator = _getEnumerator.Invoke(instance);

            if (_moveNext is null)
            {
                Type enumeratorType = enumerator.GetType();
                _moveNext = AccessTools.Method(enumeratorType, "MoveNext").CreateFastDelegate();
                _current = AccessTools.Property(enumeratorType, "Current").GetGetMethod().CreateFastDelegate();
            }

            while ((Boolean)_moveNext.Invoke(enumerator))
            {
                Object item = _current.Invoke(enumerator);
                yield return item;
            }
        }
    }
}