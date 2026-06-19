using System;

namespace PlayPane.Core.Models
{
    public sealed class CaptureSession
    {
        public CaptureSession(SourceWindowInfo source, AppSettings settings)
        {
            Id = Guid.NewGuid();
            Source = source;
            SourceKind = settings.CaptureSourceKind;
            CaptureMode = settings.CaptureMode;
            CropRegion = settings.CropRegion == null ? CropRegion.Full : settings.CropRegion.Clamp();
            FrameRateMode = settings.FrameRateMode;
            StartedAtUtc = DateTime.UtcNow;
        }

        public static CaptureSession ForBrowserExtension(AppSettings settings)
        {
            settings.CaptureSourceKind = CaptureSourceKind.BrowserExtension;
            return new CaptureSession(null, settings);
        }

        public Guid Id { get; private set; }

        public SourceWindowInfo Source { get; private set; }

        public CaptureSourceKind SourceKind { get; private set; }

        public CaptureMode CaptureMode { get; private set; }

        public CropRegion CropRegion { get; private set; }

        public FrameRateMode FrameRateMode { get; private set; }

        public DateTime StartedAtUtc { get; private set; }
    }
}
