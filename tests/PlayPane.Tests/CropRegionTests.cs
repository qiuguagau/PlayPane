using PlayPane.Core.Models;

namespace PlayPane.Tests
{
    internal static class CropRegionTests
    {
        public static void FullRegionCoversSource()
        {
            var region = CropRegion.Full;
            var pixels = region.ToPixels(1920, 1080);

            TestAssert.Equal(0, pixels.X, "Full crop should start at x=0.");
            TestAssert.Equal(0, pixels.Y, "Full crop should start at y=0.");
            TestAssert.Equal(1920, pixels.Width, "Full crop should use source width.");
            TestAssert.Equal(1080, pixels.Height, "Full crop should use source height.");
        }

        public static void PixelRegionScalesWithSourceSize()
        {
            var region = CropRegion.FromPixels(100, 50, 300, 200, 1000, 500);
            var scaled = region.ToPixels(2000, 1000);

            TestAssert.Equal(200, scaled.X, "Crop x should scale proportionally.");
            TestAssert.Equal(100, scaled.Y, "Crop y should scale proportionally.");
            TestAssert.Equal(600, scaled.Width, "Crop width should scale proportionally.");
            TestAssert.Equal(400, scaled.Height, "Crop height should scale proportionally.");
            TestAssert.Near(1.5, region.GetAspectRatio(1000, 500), 0.0001, "Crop aspect ratio should use current source pixels.");
        }

        public static void ClampKeepsRegionInsideSource()
        {
            var region = new CropRegion(0.8, 0.9, 0.5, 0.4).Clamp();
            var pixels = region.ToPixels(1000, 1000);

            TestAssert.Equal(500, pixels.Width, "Clamp should keep requested width when possible.");
            TestAssert.Equal(400, pixels.Height, "Clamp should keep requested height when possible.");
            TestAssert.Equal(500, pixels.X, "Clamp should move x inside source bounds.");
            TestAssert.Equal(600, pixels.Y, "Clamp should move y inside source bounds.");
        }
    }
}
