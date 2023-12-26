using System;
using System.Linq;
using Memoria.Persona5T.Configuration.Hotkey;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Memoria.Persona5T.Core;

public static class InputManager
{
    public static KeyControl GetKeyControl(String name) => Keyboard.current.GetChildControl(name).Cast<KeyControl>();
    public static KeyControl GetKeyControl(KeyCode keyCode) => GetKeyControl(keyCode.ToString());
    public static Boolean InputGetKey(KeyCode keyCode) => GetKeyControl(keyCode).isPressed;
    public static Boolean InputGetKeyDown(KeyCode keyCode) => GetKeyControl(keyCode).wasPressedThisFrame;
    public static Boolean InputGetKeyUp(KeyCode keyCode) => GetKeyControl(keyCode).wasReleasedThisFrame;
    
    public static Boolean GetKey(KeyCode keyCode) => Check(keyCode, InputGetKey);
    public static Boolean GetKeyDown(KeyCode keyCode) => Check(keyCode, InputGetKeyDown);
    public static Boolean GetKeyUp(KeyCode keyCode) => Check(keyCode, InputGetKeyUp);

    public static Boolean IsToggled(Hotkey hotkey)
    {
        if (!GetKeyUp(hotkey.Key))
            return false;

        return IsModifiersPressed(hotkey);
    }
    
    public static Boolean IsHold(Hotkey hotkey)
    {
        if (!GetKey(hotkey.Key))
            return false;

        return IsModifiersPressed(hotkey);
    }

    private static Boolean IsModifiersPressed(Hotkey hotkey)
    {
        if (hotkey.Control)
        {
            if (!(InputGetKey(KeyCode.LeftControl) || InputGetKey(KeyCode.RightControl)))
                return false;
        }

        if (hotkey.Alt)
        {
            if (!(InputGetKey(KeyCode.LeftAlt) || InputGetKey(KeyCode.RightAlt)))
                return false;
        }

        if (hotkey.Shift)
        {
            if (!(InputGetKey(KeyCode.LeftShift) || InputGetKey(KeyCode.RightShift)))
                return false;
        }

        return hotkey.ModifierKeys.All(InputGetKey);
    }

    public static Boolean GetKey(String action) => Check(action, InputGetKey);
    public static Boolean GetKeyDown(String action) => Check(action, InputGetKeyDown);
    public static Boolean GetKeyUp(String action) => Check(action, InputGetKeyUp);

    private static Boolean Check(KeyCode keyCode, Func<KeyCode, Boolean> checker)
    {
        return keyCode != KeyCode.None && checker(keyCode);
    }
    
    private static Boolean Check(String action, Func<KeyCode, Boolean> checker)
    {
        if (action == "None")
            return false;

        // List<KeyValue> values = InputListener.Instance.KeyConfig.GetKeyValues(action);
        //
        // foreach (var value in values)
        // {
        //     if (checker(value.KeyCode))
        //         return true;
        // }
        
        return false;
    }
}