using System;

namespace PlayPane.Core.Models
{
    public sealed class SourceWindowInfo
    {
        public SourceWindowInfo()
        {
        }

        public SourceWindowInfo(long handleValue, string title, string processName, BrowserType browserType, WindowBounds bounds, bool isMinimized, bool isVisible, string monitorDeviceName)
        {
            HandleValue = handleValue;
            Title = title;
            ProcessName = processName;
            BrowserType = browserType;
            Bounds = bounds;
            IsMinimized = isMinimized;
            IsVisible = isVisible;
            MonitorDeviceName = monitorDeviceName;
        }

        public long HandleValue { get; set; }

        public IntPtr Handle
        {
            get { return new IntPtr(HandleValue); }
        }

        public string Title { get; set; }

        public string ProcessName { get; set; }

        public BrowserType BrowserType { get; set; }

        public WindowBounds Bounds { get; set; }

        public bool IsMinimized { get; set; }

        public bool IsVisible { get; set; }

        public string MonitorDeviceName { get; set; }

        public string DisplayName
        {
            get { return BrowserType + " - " + Title; }
        }
    }
}
