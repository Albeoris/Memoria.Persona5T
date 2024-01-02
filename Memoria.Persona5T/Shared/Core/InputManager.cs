using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Memoria.Persona5T.BeepInEx;
using Memoria.Persona5T.Configuration.Hotkey;

namespace Memoria.Persona5T.Core;

public static class InputManager
{
    //private static Keyboard _keyboard;
    //private static KeyboardUpdate[] _bufferedData;
    private static Dictionary<WindowsKey, KeyState> _keyboardUpdates = new();
    private static readonly WindowsKey[] _allWindowsKeys = Enum.GetValues<WindowsKey>().DistinctBy2(v => (Int32)v).ToArray();

    [DllImport("user32.dll")]
    private static extern UInt16 GetAsyncKeyState(WindowsKey vKey);

    public static Boolean IsPressed(WindowsKey key) => (GetAsyncKeyState(key) & 0x8000) == 0x8000;


    // public static Keyboard Keyboard => _keyboard ??= Keyboard.current;
    //
    // public static KeyControl GetKeyControl(String name) => Keyboard.GetChildControl(name).Cast<KeyControl>();
    // public static KeyControl GetKeyControl(KeyCode keyCode) => GetKeyControl(keyCode.ToString());
    public static Boolean InputGetKey(WindowsKey keyCode) => _keyboardUpdates.TryGetValue(keyCode, out var update) && update.IsPressed;
    public static Boolean InputGetKeyDown(WindowsKey keyCode) => _keyboardUpdates.TryGetValue(keyCode, out var update) && update.IsDown;
    public static Boolean InputGetKeyUp(WindowsKey keyCode) => _keyboardUpdates.TryGetValue(keyCode, out var update) && update.IsUp;
    
    public static Boolean GetKey(WindowsKey keyCode) => Check(keyCode, InputGetKey);
    public static Boolean GetKeyDown(WindowsKey keyCode) => Check(keyCode, InputGetKeyDown);
    public static Boolean GetKeyUp(WindowsKey keyCode) => Check(keyCode, InputGetKeyUp);

    public static void Update()
    {
        if (_keyboardUpdates.Count == 0)
        {
            foreach (WindowsKey key in _allWindowsKeys)
                _keyboardUpdates.Add(key, new KeyState());
        }

        foreach (WindowsKey key in _allWindowsKeys)
        {
            KeyState previousState = _keyboardUpdates[key];
            previousState.IsUp = false;
            previousState.IsDown = false;
            Boolean isPressed = IsPressed(key);
            if (previousState.IsPressed)
            {
                previousState.IsUp = !isPressed;
            }
            else if (isPressed)
            {
                previousState.IsDown = true;
            }

            previousState.IsPressed = isPressed;
        }
    }

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
            if (!(InputGetKey(WindowsKey.LeftControl) || InputGetKey(WindowsKey.RightControl)))
                return false;
        }

        if (hotkey.Alt)
        {
            if (!(InputGetKey(WindowsKey.LeftAlt) || InputGetKey(WindowsKey.RightAlt)))
                return false;
        }

        if (hotkey.Shift)
        {
            if (!(InputGetKey(WindowsKey.LeftShift) || InputGetKey(WindowsKey.RightShift)))
                return false;
        }

        return hotkey.ModifierKeys.All(InputGetKey);
    }

    public static Boolean GetKey(String action) => Check(action, InputGetKey);
    public static Boolean GetKeyDown(String action) => Check(action, InputGetKeyDown);
    public static Boolean GetKeyUp(String action) => Check(action, InputGetKeyUp);

    private static Boolean Check(WindowsKey keyCode, Func<WindowsKey, Boolean> checker)
    {
        return keyCode != WindowsKey.None && checker(keyCode);
    }
    
    private static Boolean Check(String action, Func<WindowsKey, Boolean> checker)
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