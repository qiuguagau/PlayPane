using System;
using System.Collections.Generic;
using PlayPane.Core.Models;
using PlayPane.Core.Native;

namespace PlayPane.Core.Input
{
    public sealed class GlobalHotkeyService : IDisposable
    {
        private readonly Dictionary<int, HotkeyAction> _registeredActions = new Dictionary<int, HotkeyAction>();
        private IntPtr _windowHandle;
        private int _nextId = 0x5100;

        public event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;

        public IReadOnlyList<HotkeyAction> Register(IntPtr windowHandle, IEnumerable<ShortcutBinding> shortcuts)
        {
            UnregisterAll();

            _windowHandle = windowHandle;
            var failed = new List<HotkeyAction>();
            if (windowHandle == IntPtr.Zero || shortcuts == null)
            {
                return failed;
            }

            foreach (ShortcutBinding binding in shortcuts)
            {
                if (binding == null || binding.Shortcut == null || !binding.Shortcut.IsValid)
                {
                    continue;
                }

                int id = _nextId++;
                uint modifiers = ToWin32Modifiers(binding.Shortcut.Modifiers);
                uint keyCode = (uint)binding.Shortcut.KeyCode;

                if (Win32Api.RegisterHotKey(windowHandle, id, modifiers, keyCode))
                {
                    _registeredActions[id] = binding.Action;
                }
                else
                {
                    failed.Add(binding.Action);
                }
            }

            return failed;
        }

        public bool ProcessWindowMessage(int message, IntPtr wParam)
        {
            if (message != Win32Api.WM_HOTKEY)
            {
                return false;
            }

            int id = wParam.ToInt32();
            HotkeyAction action;
            if (!_registeredActions.TryGetValue(id, out action))
            {
                return false;
            }

            EventHandler<HotkeyPressedEventArgs> handler = HotkeyPressed;
            if (handler != null)
            {
                handler(this, new HotkeyPressedEventArgs(action));
            }

            return true;
        }

        public void UnregisterAll()
        {
            if (_windowHandle == IntPtr.Zero)
            {
                _registeredActions.Clear();
                return;
            }

            foreach (int id in new List<int>(_registeredActions.Keys))
            {
                Win32Api.UnregisterHotKey(_windowHandle, id);
            }

            _registeredActions.Clear();
            _windowHandle = IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterAll();
        }

        private static uint ToWin32Modifiers(HotkeyModifier modifiers)
        {
            uint result = 0;
            if ((modifiers & HotkeyModifier.Alt) == HotkeyModifier.Alt)
            {
                result |= Win32Api.MOD_ALT;
            }

            if ((modifiers & HotkeyModifier.Control) == HotkeyModifier.Control)
            {
                result |= Win32Api.MOD_CONTROL;
            }

            if ((modifiers & HotkeyModifier.Shift) == HotkeyModifier.Shift)
            {
                result |= Win32Api.MOD_SHIFT;
            }

            if ((modifiers & HotkeyModifier.Windows) == HotkeyModifier.Windows)
            {
                result |= Win32Api.MOD_WIN;
            }

            return result;
        }
    }
}
