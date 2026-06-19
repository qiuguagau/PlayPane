using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PlayPane.Core.Models;
using PlayPane.Core.Native;

namespace PlayPane.Core.Windowing
{
    public sealed class WindowEnumerator
    {
        public IList<SourceWindowInfo> Enumerate(bool showAllWindows)
        {
            var windows = new List<SourceWindowInfo>();

            Win32Api.EnumWindows(delegate(IntPtr handle, IntPtr lParam)
            {
                SourceWindowInfo info = CreateWindowInfo(handle);
                if (WindowClassifier.ShouldInclude(info, showAllWindows))
                {
                    windows.Add(info);
                }

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public SourceWindowInfo FindByPreviousSource(PreviousSourceWindow previous)
        {
            if (previous == null)
            {
                return null;
            }

            foreach (SourceWindowInfo window in Enumerate(true))
            {
                if (window.BrowserType != previous.BrowserType)
                {
                    continue;
                }

                if (!EqualsIgnoreCase(window.ProcessName, previous.ProcessName))
                {
                    continue;
                }

                if (ContainsSimilarTitle(window.Title, previous.Title))
                {
                    return window;
                }
            }

            return null;
        }

        private static SourceWindowInfo CreateWindowInfo(IntPtr handle)
        {
            bool visible = Win32Api.IsWindowVisible(handle);
            string title = GetTitle(handle);
            string processName = GetProcessName(handle);
            WindowBounds bounds = GetBounds(handle);
            BrowserType browserType = WindowClassifier.ClassifyBrowser(processName, title);
            bool minimized = Win32Api.IsIconic(handle);
            string monitor = GetMonitorName(bounds);

            return new SourceWindowInfo(handle.ToInt64(), title, processName, browserType, bounds, minimized, visible, monitor);
        }

        private static string GetTitle(IntPtr handle)
        {
            int length = Win32Api.GetWindowTextLength(handle);
            if (length <= 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(length + 1);
            Win32Api.GetWindowText(handle, builder, builder.Capacity);
            return builder.ToString();
        }

        private static string GetProcessName(IntPtr handle)
        {
            try
            {
                uint processId;
                Win32Api.GetWindowThreadProcessId(handle, out processId);
                if (processId == 0)
                {
                    return string.Empty;
                }

                return Process.GetProcessById((int)processId).ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static WindowBounds GetBounds(IntPtr handle)
        {
            Win32Api.RECT rect;
            if (!Win32Api.GetWindowRect(handle, out rect))
            {
                return new WindowBounds();
            }

            return new WindowBounds(rect.Left, rect.Top, Math.Max(0, rect.Right - rect.Left), Math.Max(0, rect.Bottom - rect.Top));
        }

        private static string GetMonitorName(WindowBounds bounds)
        {
            if (bounds == null || bounds.Width <= 0 || bounds.Height <= 0)
            {
                return string.Empty;
            }

            Rectangle rectangle = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            return Screen.FromRectangle(rectangle).DeviceName;
        }

        private static bool EqualsIgnoreCase(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsSimilarTitle(string current, string previous)
        {
            if (string.IsNullOrWhiteSpace(previous))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(current))
            {
                return false;
            }

            return current.IndexOf(previous, StringComparison.OrdinalIgnoreCase) >= 0 ||
                previous.IndexOf(current, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
