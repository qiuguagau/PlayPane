using System.Runtime.Serialization;

namespace PlayPane.Core.Models
{
    [DataContract]
    public sealed class SourcePlacementOptions
    {
        public SourcePlacementOptions()
        {
            Mode = SourcePlacementMode.KeepOriginalPosition;
            Edge = ScreenEdge.Right;
            MonitorAnchor = MonitorAnchor.Center;
            VisiblePixels = 32;
        }

        [DataMember(Order = 1)]
        public SourcePlacementMode Mode { get; set; }

        [DataMember(Order = 2)]
        public ScreenEdge Edge { get; set; }

        [DataMember(Order = 3)]
        public string TargetMonitorDeviceName { get; set; }

        [DataMember(Order = 4)]
        public MonitorAnchor MonitorAnchor { get; set; }

        [DataMember(Order = 5)]
        public int VisiblePixels { get; set; }
    }
}
