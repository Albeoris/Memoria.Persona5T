using System;
using System.Collections.Generic;

namespace Memoria.Persona5T.Configuration.Hotkey;

public sealed class Hotkey
{
    public static Hotkey None => new Hotkey(WindowsKey.None);
    
    public Boolean MustHeld { get; set; }

    public WindowsKey Key { get; } = WindowsKey.None;
    public String Action { get; } = "None";

    public Hotkey(WindowsKey key)
    {
        Key = key;
    }

    public Hotkey(String action)
    {
        if (String.IsNullOrWhiteSpace(action) || action.Equals("None", StringComparison.InvariantCultureIgnoreCase))
            action = "None";
        
        Action = action;
    }
    
    public Boolean Alt { get; set; }
    public Boolean Shift { get; set; }
    public Boolean Control { get; set; }
    
    public IReadOnlyList<WindowsKey> ModifierKeys { get; set; } = Array.Empty<WindowsKey>();
    public IReadOnlyList<String> ModifierActions { get; set; } = Array.Empty<String>();
}