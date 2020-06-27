﻿using Kingmaker;
using Kingmaker.UI;
using Kingmaker.UI.SettingsUI;
using System;
using UnityEngine;

namespace ModMaker.Utils
{
    public static class HotkeyHelper
    {
        public static bool CanBeRegistered(string bindingName, BindingKeysData bindingKey, 
            KeyboardAccess.GameModesGroup gameMode = KeyboardAccess.GameModesGroup.World)
        {
            bool isBound = Game.Instance.Keyboard.GetBindingByName(bindingName) != null;

            if (isBound)
                Game.Instance.Keyboard.UnregisterBinding(bindingName);

            bool result = Game.Instance.Keyboard.CanBeRegistered(
                bindingName,
                bindingKey.Key,
                gameMode,
                bindingKey.IsCtrlDown,
                bindingKey.IsAltDown,
                bindingKey.IsShiftDown);

            if (isBound)
                Game.Instance.Keyboard.RegisterBinding(
                    bindingName, bindingKey, gameMode, false);

            return result;
        }

        public static string GetKeyText(BindingKeysData bindingKey)
        {
            if (bindingKey == null)
            {
                return "None";
            }
            else
            {
                string prefix = 
                    bindingKey.IsCtrlDown ? "Ctrl+" :
                    bindingKey.IsShiftDown ? "Shift+" :
                    bindingKey.IsAltDown ? "Alt+" : string.Empty;

                return prefix + bindingKey.Key.ToString();
            }
        }

        public static bool ReadKey(out BindingKeysData bindingKey)
        {
            KeyCode keyCode = KeyCode.None;

            foreach (KeyCode keyHeld in Enum.GetValues(typeof(KeyCode)))
            {
                if (keyHeld  == KeyCode.None || keyHeld  > KeyCode.PageDown)
                    continue;

                if (Input.GetKey(keyHeld ))
                {
                    keyCode = keyHeld ;
                    break;
                }
            }

            if (keyCode != KeyCode.None)
            {
                bindingKey = new BindingKeysData()
                {
                    Key = keyCode,
                    IsCtrlDown = Input.GetKey(KeyCode.LeftControl),
                    IsAltDown = Input.GetKey(KeyCode.LeftAlt),
                    IsShiftDown = Input.GetKey(KeyCode.LeftShift)
                };
                return true;
            }
            else
            {
                bindingKey = null;
                return false;
            }
        }

        public static void RegisterKey(string bindingName, BindingKeysData bindingKey,
            KeyboardAccess.GameModesGroup gameMode = KeyboardAccess.GameModesGroup.World)
        {
            Game.Instance.Keyboard.UnregisterBinding(bindingName);

            if (bindingKey != null && bindingKey.Key != KeyCode.None)
            {
                Game.Instance.Keyboard.RegisterBinding(bindingName, bindingKey, gameMode, false);
            }
        }

        public static void UnregisterKey(string bindingName)
        {
            Game.Instance.Keyboard.UnregisterBinding(bindingName);
        }
    }
}
