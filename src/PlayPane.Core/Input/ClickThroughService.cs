using System;
using PlayPane.Core.Native;

namespace PlayPane.Core.Input
{
    public sealed class ClickThroughService
    {
        public void SetClickThrough(IntPtr windowHandle, bool enabled)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            long style = Win32Api.GetWindowLongPtr(windowHandle, Win32Api.GWL_EXSTYLE).ToInt64();

            if (enabled)
            {
                style |= Win32Api.WS_EX_TRANSPARENT;
                style |= Win32Api.WS_EX_NOACTIVATE;
                style |= Win32Api.WS_EX_APPWINDOW;
                style &= ~Win32Api.WS_EX_TOOLWINDOW;
            }
            else
            {
                style &= ~Win32Api.WS_EX_TRANSPARENT;
                style &= ~Win32Api.WS_EX_NOACTIVATE;
                style |= Win32Api.WS_EX_APPWINDOW;
                style &= ~Win32Api.WS_EX_TOOLWINDOW;
            }

            Win32Api.SetWindowLongPtr(windowHandle, Win32Api.GWL_EXSTYLE, new IntPtr(style));
            Win32Api.SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0, Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
        }
    }
}
