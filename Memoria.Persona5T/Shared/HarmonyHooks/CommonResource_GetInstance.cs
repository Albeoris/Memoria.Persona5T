using System;
using Game.Common;
using HarmonyLib;
using Memoria.Persona5T.IL2CPP;

namespace Memoria.Persona5T.HarmonyHooks;

// ReSharper disable InconsistentNaming
// ReSharper disable once StringLiteralTypo
// ReSharper disable once IdentifierTypo
[HarmonyPatch(typeof(CommonResource), nameof(CommonResource.GetInstance))]
public static class CommonResource_GetInstance
{
    public static void Postfix(CommonResource __result)
    {
        try
        {
            
        }
        catch (Exception ex)
        {
            ModComponent.Log.LogError(ex);
        }
    }
}