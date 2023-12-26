using System;

namespace Memoria.Persona5T.Configuration;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigEntryAttribute : Attribute
{
    public String Description { get; }
    
    public ConfigEntryAttribute(String description)
    {
        Description = description;
    }
}