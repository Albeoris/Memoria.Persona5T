using HarmonyLib;
using Memoria.Persona5T.IL2CPP;
using UniRx;

namespace Memoria.Persona5T.HarmonyHooks;

[HarmonyPatch(typeof(MainThreadDispatcher), nameof(MainThreadDispatcher.LateUpdate))]
public static class MainThreadDispatcher_LateUpdate
{
    public static void Prefix()
    {
        ModComponent.LateUpdate();
    }
}