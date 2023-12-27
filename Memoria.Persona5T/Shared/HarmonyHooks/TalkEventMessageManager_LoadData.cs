using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using Game.Common;
using HarmonyLib;
using Il2CppSystem.Text;
using Memoria.Persona5T.BeepInEx;
using Memoria.Persona5T.Configuration;
using Memoria.Persona5T.IL2CPP;
using PPScriptModule;
using SbLib.Localication;

namespace Memoria.Persona5T.HarmonyHooks;

[HarmonyPatch(typeof(TalkEventMessageManager), nameof(TalkEventMessageManager.LoadData))]
public static class TalkEventMessageManager_LoadData
{
    public static void Postfix(string scriptName, Il2CppSystem.IO.BinaryReader binaryReader, TalkEventMessageManager __instance)
    {
        try
        {
            TalkEventMessageTableData messageTable = GetMessageTable(__instance, scriptName);
            String currentLocale = LocalizationSettings.SelectedLocale.Identifier.Code;
            TryExport(messageTable, currentLocale);
            TryMod(messageTable, currentLocale);
        }
        catch (Exception ex)
        {
            ModComponent.Log.LogError(ex);
        }
    }

    private static void TryMod(TalkEventMessageTableData tableData, String currentLocale)
    {
        String scriptName = tableData.m_FileName;
        try
        {
            IReadOnlyList<String> modFiles = ModComponent.ModFiles.FindAll($"TalkEvents/{currentLocale}/{scriptName}.json");
            if (modFiles.Count == 0)
                return;

            Dictionary<String, TalkEventMessageTableData.Data> dic = new();

            foreach (var data in tableData.m_DataList)
                dic[data.Id] = data;

            List<TalkEventMessageTableData.Data> newData = new List<TalkEventMessageTableData.Data>();
            Int32 changed = 0;

            foreach (String file in modFiles)
            {
                String shortPath = ApplicationPathConverter.ReturnPlaceholders(file);
                using (FileStream input = File.OpenRead(file))
                {
                    Data[] list = JsonSerializer.Deserialize<Data[]>(input);
                    foreach (Data data in list)
                    {
                        Boolean isNew = false;
                        Int32 propertyNumber = 1;
                        if (!dic.TryGetValue(data.Id, out var target))
                        {
                            target = new TalkEventMessageTableData.Data { Id = data.Id };
                            dic.Add(data.Id, target);
                            newData.Add(target);
                            isNew = true;
                        }

                        if (data.NameId != null)
                        {
                            target.NameId = data.NameId.Value;
                            propertyNumber++;
                        }

                        if (data.VoiceCueSheetName != null)
                        {
                            target.VoiceCueSheetName = data.VoiceCueSheetName;
                            propertyNumber++;
                        }

                        if (data.VoiceId != null)
                        {
                            target.VoiceId = data.VoiceId;
                            propertyNumber++;
                        }

                        if (data.WindowType != null)
                        {
                            target.WindowType = data.WindowType.Value;
                            propertyNumber++;
                        }

                        if (data.EmoId != null)
                        {
                            target.EmoId = data.EmoId.Value;
                            propertyNumber++;
                        }

                        if (data.EmoSize != null)
                        {
                            target.EmoSize = data.EmoSize.Value;
                            propertyNumber++;
                        }

                        if (data.Message != null)
                        {
                            target.Message = data.Message;
                            propertyNumber++;
                        }

                        if (propertyNumber > 1)
                            changed++;

                        if (isNew && propertyNumber == 8)
                            throw new NotSupportedException($"Failed to add entry [{data.Id}]. There is only {propertyNumber} properties from 8 expected. File: {shortPath}");
                    }
                }
            }

            if (newData.Count > 0)
            {
                ModComponent.Log.LogInfo($"[Mod] Added {newData.Count} entries to [{scriptName}].");
                foreach (var data in newData)
                    tableData.m_DataList.Add(data);
            }

            ModComponent.Log.LogInfo($"[Mod] Changed {changed - newData.Count} entries from [{scriptName}].");
        }
        catch (Exception ex)
        {
            ModComponent.Log.LogException(ex, $"Failed to export talk event data with name [{scriptName}].");
        }
    }

    private static void TryExport(TalkEventMessageTableData tableData, String currentLocale)
    {
        String scriptName = tableData.m_FileName;

        try
        {
            String exportDirectory = ModComponent.Config.Assets.GetExportDirectoryIfEnabled();
            if (exportDirectory == String.Empty)
                return;

            Dictionary<Int32, String> characterNames = GetNameIdToNameMap();
            String outputDirectory = $@"{exportDirectory}/TalkEvents/{currentLocale}";
            Directory.CreateDirectory(outputDirectory);
            
            String outputPath = $@"{outputDirectory}/{scriptName}.json";
            String shortPath = ApplicationPathConverter.ReturnPlaceholders(outputPath);
            ModComponent.Log.LogInfo($"[Export] Exporting talk event [{scriptName}] to [{shortPath}]");

            using (Stream output = File.Create(outputPath))
            using (Utf8JsonWriter json = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                json.WriteStartArray();
                {
                    foreach (TalkEventMessageTableData.Data data in tableData.m_DataList)
                    {
                        characterNames.TryGetValue(data.NameId, out String name);
                        json.WriteStartObject();
                        {
                            json.WriteString("Id", data.Id);
                            json.WriteNumber("NameId", data.NameId);
                            json.WriteString("Name", name);
                            json.WriteString("VoiceCueSheetName", data.VoiceCueSheetName);
                            json.WriteString("VoiceId", data.VoiceId);
                            json.WriteNumber("WindowType", data.WindowType);
                            json.WriteNumber("EmoId", data.EmoId);
                            json.WriteNumber("EmoSize", data.EmoSize);
                            json.WriteString("Message", data.Message);
                        }
                        json.WriteEndObject();
                    }
                }
                json.WriteEndArray();
            }
        }
        catch (Exception ex)
        {
            ModComponent.Log.LogException(ex, $"Failed to export talk event data with name [{scriptName}].");
        }
    }

    private static Dictionary<Int32, String> GetNameIdToNameMap()
    {
        CommonResource commonResource = CommonResource.GetInstance();
        Dictionary<Int32, String> characterNames = new(commonResource.m_DatabasePack.m_CharaNameDatabase.param.Count);
        foreach (var nameToWord in commonResource.m_DatabasePack.m_CharaNameDatabase.param)
        {
            if (nameToWord.NamePlateWordKey < 1)
                continue;
                
            String word = commonResource.GetWord(nameToWord.NamePlateWordKey);
            characterNames[nameToWord.NameId] = word;
        }

        return characterNames;
    }

    private static TalkEventMessageTableData GetMessageTable(TalkEventMessageManager manager, String scriptName)
    {
        StringBuilder sb = new StringBuilder();

        foreach (TalkEventMessageTableData tableData in manager.m_MessageTableList)
        {
            if (tableData.m_FileName == scriptName)
                return tableData;

            sb.Append(tableData.m_FileName);
            sb.Append("; ");
        }

        throw new InvalidOperationException($"Cannot find talk event data with name [{scriptName}]. Loaded table names: [{sb.ToString()}]");
    }

    private sealed class Data
    {
        public String Id { get; init; }
        public Int32? NameId { get; init; }
        public String VoiceCueSheetName { get; init; }
        public String VoiceId { get; init; }
        public Int32? WindowType { get; init; }
        public Int32? EmoId { get; init; }
        public Single? EmoSize { get; init; }
        public String Message { get; init; }
    }
}