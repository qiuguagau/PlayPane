using System.Drawing;
using PlayPane.Core.Models;

namespace PlayPane.Core.Capture
{
    public interface IWindowCaptureService
    {
        Bitmap Capture(SourceWindowInfo source, CaptureMode mode, CropRegion cropRegion);
    }
}
