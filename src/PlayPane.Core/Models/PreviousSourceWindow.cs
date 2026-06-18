using System.Runtime.Serialization;

namespace PlayPane.Core.Models
{
    [DataContract]
    public sealed class PreviousSourceWindow
    {
        public PreviousSourceWindow()
        {
        }

        public PreviousSourceWindow(string processName, BrowserType browserType, string title, WindowBounds bounds)
        {
            ProcessName = processName;
            BrowserType = browserType;
            Title = title;
            Bounds = bounds;
        }

        [DataMember(Order = 1)]
        public string ProcessName { get; set; }

        [DataMember(Order = 2)]
        public BrowserType BrowserType { get; set; }

        [DataMember(Order = 3)]
        public string Title { get; set; }

        [DataMember(Order = 4)]
        public WindowBounds Bounds { get; set; }
    }
}
