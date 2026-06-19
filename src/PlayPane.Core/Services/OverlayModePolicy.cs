namespace PlayPane.Core.Services
{
    public sealed class OverlayModePolicy
    {
        private OverlayModePolicy(bool isTopmost, bool isClickThrough)
        {
            IsTopmost = isTopmost;
            IsClickThrough = isClickThrough;
        }

        public bool IsTopmost { get; private set; }

        public bool IsClickThrough { get; private set; }

        public static OverlayModePolicy ForEditMode()
        {
            return new OverlayModePolicy(false, false);
        }

        public static OverlayModePolicy ForGameMode()
        {
            return new OverlayModePolicy(true, true);
        }
    }
}
