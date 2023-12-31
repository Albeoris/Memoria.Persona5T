﻿using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using Memoria.Persona5T.Core;

namespace Memoria.Persona5T.Configuration;

public sealed class ConfigFileProvider
{
    private readonly Dictionary<String, ConfigFile> _cache = new();

    public ConfigFile GetAndCache(String sectionName)
    {
        if (!_cache.TryGetValue(sectionName, out var file))
        {
            file = Get(sectionName);
            _cache.Add(sectionName, file);
        }

        return file;
    }

    private ConfigFile Get(String sectionName)
    {
        String configPath = GetConfigurationPath(sectionName);
        return new ConfigFile(configPath, true, ownerMetadata: null);
    }
        
    private static String GetConfigurationPath(String sectionName)
    {
        return Path.Combine(Paths.ConfigPath, ModConstants.Id, sectionName + ".cfg");
    }
}