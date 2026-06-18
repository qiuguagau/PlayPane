using System;
using PlayPane.Core.Models;

namespace PlayPane.Core.Windowing
{
    public static class WindowClassifier
    {
        public static BrowserType ClassifyBrowser(string processName, string title)
        {
            string process = (processName ?? string.Empty).Trim().ToLowerInvariant();

            if (process == "chrome")
            {
                return BrowserType.Chrome;
            }

            if (process == "firefox")
            {
                return BrowserType.Firefox;
            }

            if (process == "msedge" || process == "microsoftedge")
            {
                return BrowserType.Edge;
            }

            return BrowserType.Other;
        }

        public static bool ShouldInclude(SourceWindowInfo window, bool showAllWindows)
        {
            if (window == null)
            {
                return false;
            }

            if (!window.IsVisible || window.Bounds == null || !window.Bounds.IsUsable)
            {
                return false;
            }

            if (IsPlayPaneWindow(window))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(window.Title))
            {
                return false;
            }

            if (showAllWindows)
            {
                return true;
            }

            return window.BrowserType == BrowserType.Chrome ||
                window.BrowserType == BrowserType.Firefox ||
                window.BrowserType == BrowserType.Edge;
        }

        private static bool IsPlayPaneWindow(SourceWindowInfo window)
        {
            return Contains(window.ProcessName, "playpane") || Contains(window.Title, "playpane");
        }

        private static bool Contains(string value, string token)
        {
            return value != null && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
