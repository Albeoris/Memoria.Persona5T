using System;

namespace Memoria.Persona5T.Core;

public sealed record class KeyState
{
    public Boolean IsPressed { get; set; }
    public Boolean IsDown { get; set; }
    public Boolean IsUp { get; set; }
}