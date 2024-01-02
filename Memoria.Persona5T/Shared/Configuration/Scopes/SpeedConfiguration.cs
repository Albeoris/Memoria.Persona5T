using System;
using Memoria.Persona5T.Configuration.Hotkey;

namespace Memoria.Persona5T.Configuration;

[ConfigScope("Speed")]
public abstract partial class SpeedConfiguration
{
    private const String Section = "Speed";

    [ConfigEntry($"Speed up key.")]
    [ConfigConverter(nameof(KeyConverter))]
    public virtual HotkeyGroup Key { get; } = HotkeyGroup.Create(new[] { new Hotkey.Hotkey(WindowsKey.F1), new Hotkey.Hotkey(WindowsKey.F1) { MustHeld = true } });

    [ConfigEntry($"Speed up toggle factor.")]
    public virtual Single ToggleFactor { get; } = 3.0f;

    [ConfigEntry($"Speed up hold factor.")]
    public virtual Single HoldFactor { get; } = 5.0f;

    public abstract void CopyFrom(SpeedConfiguration configuration);

    protected IAcceptableValue<HotkeyGroup> KeyConverter { get; } = new AcceptableHotkeyGroup(nameof(Key), canHold: true);
}