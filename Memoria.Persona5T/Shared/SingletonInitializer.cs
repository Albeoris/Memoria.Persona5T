using System;
using BepInEx.Logging;
using Memoria.Persona5T.IL2CPP;
using UniRx;
using UnityEngine;

namespace Memoria.Persona5T;

public sealed class SingletonInitializer
{
    private readonly ManualLogSource _log;

    public SingletonInitializer(ManualLogSource logSource)
    {
        _log = logSource ?? throw new ArgumentNullException(nameof(logSource));
    }
        
    public void InitializeInGameSingleton()
    {
        try
        {
            String name = typeof(ModComponent).FullName;
            _log.LogInfo($"Initializing in-game singleton: {name}");
            
            GameObject singletonObject = new GameObject(name);
            singletonObject.hideFlags = HideFlags.HideAndDontSave;
            GameObject.DontDestroyOnLoad(singletonObject);
            singletonObject.AddComponent<MainThreadDispatcher>(); // We cannot subscribe here - it'll lead to crash; using hooks of MainThreadDispatcher methods instead  
            ModComponent.Awake();
            
            _log.LogInfo("In-game singleton initialized successfully.");
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to initialize in-game singleton.", ex);
        }
    }
}