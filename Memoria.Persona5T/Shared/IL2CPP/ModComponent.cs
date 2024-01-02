using System;
using BepInEx.Logging;
using Memoria.Persona5T.Configuration;
using Memoria.Persona5T.Core;
using Memoria.Persona5T.Mods;
using Exception = System.Exception;
using Logger = BepInEx.Logging.Logger;

namespace Memoria.Persona5T.IL2CPP;

public static class ModComponent
{
    public static ManualLogSource Log;

    [field: NonSerialized] public static ModConfiguration Config;
    [field: NonSerialized] public static ModFileResolver ModFiles;
    [field: NonSerialized] public static GameSpeedControl SpeedControl;

    private static Boolean _isDisabled;

    public static void Awake()
    {
        Log = Logger.CreateLogSource("Memoria IL2CPP");
        Log.LogMessage($"[{nameof(ModComponent)}].{nameof(Awake)}(): Begin...");
        try
        {
            Config = new ModConfiguration();
            ModFiles = new ModFileResolver();
            SpeedControl = new GameSpeedControl();
    
            Log.LogMessage($"[{nameof(ModComponent)}].{nameof(Awake)}(): Processed successfully.");
        }
        catch (Exception ex)
        {
            _isDisabled = true;
            Log.LogError($"[{nameof(ModComponent)}].{nameof(Awake)}(): {ex}");
            throw;
        }
    }
    
    public static void OnDestroy()
    {
        Log.LogInfo($"[{nameof(ModComponent)}].{nameof(OnDestroy)}()");
    }
    
    public static void Update()
    {
        try
        {
            if (_isDisabled)
                return;

            ModFiles.TryUpdate();
        }
        catch (Exception ex)
        {
            _isDisabled = true;
            Log.LogError($"[{nameof(ModComponent)}].{nameof(Update)}(): {ex}");
        }
    }

    public static void LateUpdate()
    {
        try
        {
            if (_isDisabled)
                return;

            InputManager.Update();
            SpeedControl.TryUpdate();//
        }
        catch (Exception ex)
        {
            _isDisabled = true;
            Log.LogError($"[{nameof(ModComponent)}].{nameof(LateUpdate)}(): {ex}");
        }
    }
}