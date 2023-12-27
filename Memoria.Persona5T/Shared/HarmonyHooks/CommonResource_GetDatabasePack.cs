using System;
using System.Runtime.Serialization.Json;
using Game.Common;
using HarmonyLib;
using Memoria.Persona5T.IL2CPP;
using TalkEvent;

namespace Memoria.Persona5T.HarmonyHooks;

[HarmonyPatch(typeof(CommonResource), nameof(CommonResource.GetDatabasePack))]
public static class CommonResource_GetDatabasePack
{
    // TODO
    // battlenavi_database.csv
    // effect_database.csv
    // Entity_achievement_database.csv
    // Entity_constant_database.csv
    // Entity_memo_database.csv
    // Entity_navi_skill_database.csv
    // Entity_pb_activities_database.csv
    // Entity_result_ui_database.csv
    // Entity_sound_database.csv
    // Entity_unit_sound_database.csv
    // tp_behavior_database.csv
    // unit_ai_database.csv
    // unit_ai_skill_score_database.csv
    // unit_buff_database.csv
    public static void Postfix(CommonResource.DatabasePack __result)
    {
        try
        {
            CommonResource commonResource = CommonResource.GetInstance();
            DatabaseManager<CommonResource>.OnCall(commonResource, commonResource.GetHashCode(), "Common");
            DatabaseManager<CommonResource.DatabasePack>.OnCall(__result, __result.GetHashCode(), "Data");
        }
        catch (Exception ex)
        {
            ModComponent.Log.LogError(ex);
        }
    }
}