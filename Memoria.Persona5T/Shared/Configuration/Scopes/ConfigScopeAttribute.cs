﻿using System;

namespace Memoria.Persona5T.Configuration;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ConfigScopeAttribute : Attribute
{
    public String SectionName { get; }
    
    public ConfigScopeAttribute(String sectionName)
    {
        SectionName = sectionName;
    }
}