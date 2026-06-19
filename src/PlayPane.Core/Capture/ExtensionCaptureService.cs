using System.Drawing;
using System.Drawing.Imaging;
using PlayPane.Core.Models;
using PlayPane.Core.Services;

namespace PlayPane.Core.Capture
{
    public sealed class ExtensionCaptureService : IWindowCaptureService
    {
        private readonly ExtensionFrameStore _frameStore;
        private readonly CropProcessor _cropProcessor;

        public ExtensionCaptureService(ExtensionFrameStore frameStore)
            : this(frameStore, new CropProcessor())
        {
        }

        public ExtensionCaptureService(ExtensionFrameStore frameStore, CropProcessor cropProcessor)
        {
            _frameStore = frameStore;
            _cropProcessor = cropProcessor;
        }

        public Bitmap Capture(SourceWindowInfo source, CaptureMode mode, CropRegion cropRegion)
        {
            Bitmap frame;
            if (!_frameStore.TryGetFrame(out frame))
            {
                throw new WindowCaptureException("No browser extension frames have been received yet.");
            }

            if (mode != CaptureMode.Crop)
            {
                return frame;
            }

            try
            {
                PixelRect crop = _cropProcessor.Resolve(cropRegion, frame.Width, frame.Height);
                return frame.Clone(new Rectangle(crop.X, crop.Y, crop.Width, crop.Height), PixelFormat.Format32bppArgb);
            }
            finally
            {
                frame.Dispose();
            }
        }
    }
}
