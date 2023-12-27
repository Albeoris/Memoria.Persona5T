using HarmonyLib;
using Memoria.Persona5T.IL2CPP;
using UniRx;

namespace Memoria.Persona5T.HarmonyHooks;

[HarmonyPatch(typeof(MainThreadDispatcher), nameof(MainThreadDispatcher.Update))]
public static class MainThreadDispatcher_Update
{
    public static void Prefix()
    {
        ModComponent.Update();
    }
}