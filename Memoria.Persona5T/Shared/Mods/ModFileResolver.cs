﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Game.Common;
using Memoria.Persona5T.Configuration;
using Memoria.Persona5T.Core;
using Memoria.Persona5T.HarmonyHooks;
// using Memoria.Persona5T.HarmonyHooks;
using Memoria.Persona5T.IL2CPP;

namespace Memoria.Persona5T.Mods;

public sealed class ModFileResolver : SafeComponent
{
    private readonly Object _lock = new();
    private readonly String _modsRoot;
    
    private Dictionary<String, List<String>> _catalog;
    private Int64 _fileVersion;
    private Int64 _catalogVersion;
    private FileSystemWatcher _watcher;
    
    public ModFileResolver()
    {
        _modsRoot = ModComponent.Config.Assets.ModsDirectory;
        _catalog = IndexMods(_modsRoot);
    }

    public Int64 CurrentVersion => Interlocked.Read(ref _fileVersion);

    public IReadOnlyList<String> FindAll(String assetAddress)
    {
        if (!GetActualCatalog().TryGetValue(assetAddress, out List<String> modNames))
            return Array.Empty<String>();

        return modNames.Select(n => Path.Combine(_modsRoot, n, assetAddress)).ToArray();
    }

    public IReadOnlyList<String> FindAllStartedWith(String assetRoot)
    {
        List<String> result = new List<String>();

        foreach (KeyValuePair<String, List<String>> pair in GetActualCatalog())
        {
            (String assetPath, List<String> modNames) = (pair.Key, pair.Value);
            if (!assetPath.StartsWith(assetRoot))
                continue;
            
            foreach (String modName in modNames)
                result.Add(Path.Combine(_modsRoot, modName, assetPath));
        }

        return result;
    }
    
    protected override void Update()
    {
        RefreshWatcher();
        
        if (!TryRefreshCatalog())
            return;

        DatabaseManager<CommonResource>.Refresh("Common");
        DatabaseManager<CommonResource.DatabasePack>.Refresh("Data");
    }

    private Boolean TryRefreshCatalog()
    {
        if (!ModComponent.Config.Assets.ModsEnabled)
            return false;

        Int64 fileVersion = Interlocked.Read(ref _fileVersion);
        Int64 currentVersion = Interlocked.Read(ref _catalogVersion);
        if (fileVersion == currentVersion)
            return false;

        while (true)
        {
            fileVersion = Interlocked.Read(ref _fileVersion);
            currentVersion = Interlocked.Read(ref _catalogVersion);
            if (fileVersion == currentVersion)
                break;

            _catalog = IndexMods(_modsRoot);

            _catalogVersion = fileVersion;
            ModComponent.Log.LogInfo($"[Mods] Mod catalog has been refreshed: {currentVersion} -> {fileVersion}");
        }

        return true;
    }

    private Dictionary<String, List<String>> GetActualCatalog()
    {
        RefreshWatcher();

        Dictionary<String, List<String>> catalog = _catalog;
        
        Int64 fileVersion = Interlocked.Read(ref _fileVersion);
        Int64 currentVersion = Interlocked.Read(ref _catalogVersion);
        if (fileVersion == currentVersion)
            return catalog;
        
        lock (_lock)
        {
            while (true)
            {
                fileVersion = Interlocked.Read(ref _fileVersion);
                currentVersion = Interlocked.Read(ref _catalogVersion);
                if (fileVersion == currentVersion)
                    break;
                    
                catalog = IndexMods(_modsRoot);
                _catalog = catalog;

                _catalogVersion = fileVersion;
                ModComponent.Log.LogInfo($"[Mods] Mod catalog has been refreshed: {currentVersion} -> {fileVersion}");
            }
        }

        return catalog;
    }

    private Dictionary<String, List<String>> IndexMods(String modsRoot)
    {
        Dictionary<String, List<String>> catalog = new(StringComparer.InvariantCultureIgnoreCase);

        if (!Directory.Exists(modsRoot))
        {
            ModComponent.Log.LogInfo($"[Mods] Mods indexing skipped. Mods directory is not defined.");
            return catalog;
        }

        String[] mods = Directory.GetDirectories(modsRoot);
        foreach (String modDirectory in mods)
        {
            String modName = Path.GetFileName(modDirectory);

            String shortPath = ApplicationPathConverter.ReturnPlaceholders(modDirectory);
            ModComponent.Log.LogInfo($"[Mods.{modName}] Indexing mod. Directory: {shortPath}.");
            String[] files = Directory.GetFiles(modDirectory, "*", SearchOption.AllDirectories);
            foreach (String file in files)
            {
                // Before: C:\Mods\My\Assets\GameAssets\File.txt
                // After:             Assets\GameAssets\File.txt
                String assetAddress = file.Substring(modDirectory.Length + 1);
                
                // Before: Assets\GameAssets\File.txt
                // After : Assets/GameAssets/File.txt
                assetAddress = assetAddress.Replace("\\", "/");
                if (!catalog.TryGetValue(assetAddress, out var modNames))
                {
                    modNames = new List<String>();
                    catalog.Add(assetAddress, modNames);
                }

                modNames.Add(modName);
                ModComponent.Log.LogInfo($"[Mods.{modName}] {assetAddress}.");
            }
        }

        RefreshWatcher();

        return catalog;
    }

    private void RefreshWatcher()
    {
        Boolean watchingEnabled = ModComponent.Config.Assets.ModsEnabled && ModComponent.Config.Assets.WatchingEnabled;
        FileSystemWatcher watcher = _watcher;
        if (watcher != null)
        {
            if (watcher.EnableRaisingEvents)
            {
                if (!watchingEnabled)
                {
                    watcher.EnableRaisingEvents = false;
                    ModComponent.Log.LogInfo($"[Mods] Directory watching disabled.");
                }
            }
            else if (watchingEnabled)
            {
                watcher.EnableRaisingEvents = true;
                ModComponent.Log.LogInfo($"[Mods] Directory watching enabled.");
            }
        }
        else if (watchingEnabled)
        {
            lock (_lock)
            {
                if (watcher is null)
                {
                    watcher = new FileSystemWatcher(_modsRoot);
                    watcher.IncludeSubdirectories = true;
                    watcher.Created += (sender, args) => Interlocked.Increment(ref _fileVersion);
                    watcher.Changed += (sender, args) => Interlocked.Increment(ref _fileVersion);
                    watcher.Renamed += (sender, args) => Interlocked.Increment(ref _fileVersion);
                    watcher.Deleted += (sender, args) => Interlocked.Increment(ref _fileVersion);
                    watcher.EnableRaisingEvents = true;
                    _watcher = watcher;
                    ModComponent.Log.LogInfo($"[Mods] Directory watching started.");
                }
            }
        }
    }
}