using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlayPane.Core.Models
{
    [DataContract]
    public sealed class ShortcutDefinition
    {
        public ShortcutDefinition()
        {
        }

        public ShortcutDefinition(HotkeyModifier modifiers, int keyCode)
        {
            Modifiers = modifiers;
            KeyCode = keyCode;
        }

        [DataMember(Order = 1)]
        public HotkeyModifier Modifiers { get; set; }

        [DataMember(Order = 2)]
        public int KeyCode { get; set; }

        public bool IsValid
        {
            get { return KeyCode > 0 && Modifiers != HotkeyModifier.None; }
        }

        public string ToDisplayString()
        {
            var parts = new List<string>();

            if ((Modifiers & HotkeyModifier.Control) == HotkeyModifier.Control)
            {
                parts.Add("Ctrl");
            }

            if ((Modifiers & HotkeyModifier.Alt) == HotkeyModifier.Alt)
            {
                parts.Add("Alt");
            }

            if ((Modifiers & HotkeyModifier.Shift) == HotkeyModifier.Shift)
            {
                parts.Add("Shift");
            }

            if ((Modifiers & HotkeyModifier.Windows) == HotkeyModifier.Windows)
            {
                parts.Add("Win");
            }

            parts.Add(KeyName(KeyCode));
            return string.Join(" + ", parts.ToArray());
        }

        private static string KeyName(int keyCode)
        {
            if (keyCode >= 65 && keyCode <= 90)
            {
                return ((char)keyCode).ToString();
            }

            if (keyCode == 38)
            {
                return "Up";
            }

            if (keyCode == 40)
            {
                return "Down";
            }

            return "Key " + keyCode;
        }
    }
}
