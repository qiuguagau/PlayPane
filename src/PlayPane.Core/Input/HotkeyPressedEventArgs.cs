using System;
using PlayPane.Core.Models;

namespace PlayPane.Core.Input
{
    public sealed class HotkeyPressedEventArgs : EventArgs
    {
        public HotkeyPressedEventArgs(HotkeyAction action)
        {
            Action = action;
        }

        public HotkeyAction Action { get; private set; }
    }
}
