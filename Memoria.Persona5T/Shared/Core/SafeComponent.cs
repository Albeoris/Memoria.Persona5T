﻿using System;
using Memoria.Persona5T.IL2CPP;

namespace Memoria.Persona5T.Core;

public abstract class SafeComponent
{
    private Boolean _isDisabled;

    protected virtual void Update()
    {
    }

    public void TryUpdate()
    {
        try
        {
            if (_isDisabled)
                return;

            Update();
        }
        catch (Exception ex)
        {
            _isDisabled = true;
            ModComponent.Log.LogError($"[{GetType().Name}].{nameof(Update)}(): {ex}");
        }   
    }
}