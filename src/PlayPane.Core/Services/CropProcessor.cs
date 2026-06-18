using PlayPane.Core.Models;

namespace PlayPane.Core.Services
{
    public sealed class CropProcessor
    {
        public PixelRect Resolve(CropRegion region, int sourceWidth, int sourceHeight)
        {
            if (region == null)
            {
                region = CropRegion.Full;
            }

            return region.Clamp().ToPixels(sourceWidth, sourceHeight);
        }

        public CropRegion FromPixels(int x, int y, int width, int height, int sourceWidth, int sourceHeight)
        {
            return CropRegion.FromPixels(x, y, width, height, sourceWidth, sourceHeight);
        }
    }
}
