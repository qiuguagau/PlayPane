using System;
using System.Runtime.Serialization;

namespace PlayPane.Core.Models
{
    [DataContract]
    public sealed class WindowPlacementSnapshot
    {
        public WindowPlacementSnapshot()
        {
            CapturedAtUtc = DateTime.UtcNow;
        }

        [DataMember(Order = 1)]
        public long HandleValue { get; set; }

        [DataMember(Order = 2)]
        public string Title { get; set; }

        [DataMember(Order = 3)]
        public string ProcessName { get; set; }

        [DataMember(Order = 4)]
        public BrowserType BrowserType { get; set; }

        [DataMember(Order = 5)]
        public WindowBounds Bounds { get; set; }

        [DataMember(Order = 6)]
        public string MonitorDeviceName { get; set; }

        [DataMember(Order = 7)]
        public bool IsMaximized { get; set; }

        [DataMember(Order = 8)]
        public int ShowCommand { get; set; }

        [DataMember(Order = 9)]
        public DateTime CapturedAtUtc { get; set; }

        public IntPtr Handle
        {
            get { return new IntPtr(HandleValue); }
        }
    }
}
