namespace PlayPane.Core.Models
{
    public enum SourcePlacementMode
    {
        KeepOriginalPosition = 0,
        MoveToScreenEdge = 1,
        MoveToAnotherMonitor = 2,
        MoveMostlyOffScreen = 3
    }

    public enum ScreenEdge
    {
        Left = 0,
        Right = 1,
        Top = 2,
        Bottom = 3
    }

    public enum MonitorAnchor
    {
        TopLeft = 0,
        TopRight = 1,
        BottomLeft = 2,
        BottomRight = 3,
        Center = 4,
        Maximize = 5
    }
}
