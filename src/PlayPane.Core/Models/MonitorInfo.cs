namespace PlayPane.Core.Models
{
    public sealed class MonitorInfo
    {
        public MonitorInfo()
        {
        }

        public MonitorInfo(string deviceName, WindowBounds bounds, WindowBounds workingArea, bool isPrimary)
        {
            DeviceName = deviceName;
            Bounds = bounds;
            WorkingArea = workingArea;
            IsPrimary = isPrimary;
        }

        public string DeviceName { get; set; }

        public WindowBounds Bounds { get; set; }

        public WindowBounds WorkingArea { get; set; }

        public bool IsPrimary { get; set; }

        public override string ToString()
        {
            return DeviceName;
        }
    }
}
