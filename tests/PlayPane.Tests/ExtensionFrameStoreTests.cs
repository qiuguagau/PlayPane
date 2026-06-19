using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PlayPane.Core.Capture;

namespace PlayPane.Tests
{
    internal static class ExtensionFrameStoreTests
    {
        public static void StoresAndReturnsLatestFrame()
        {
            var store = new ExtensionFrameStore();
            store.Update(CreateImageBytes(16, 9));

            Bitmap frame;
            bool found = store.TryGetFrame(out frame);

            TestAssert.True(found, "Frame store should report a frame after update.");
            using (frame)
            {
                TestAssert.Equal(16, frame.Width, "Stored frame width should round-trip.");
                TestAssert.Equal(9, frame.Height, "Stored frame height should round-trip.");
            }
        }

        public static void EmptyStoreHasNoFrame()
        {
            var store = new ExtensionFrameStore();

            Bitmap frame;
            bool found = store.TryGetFrame(out frame);

            TestAssert.False(found, "Empty frame store should report no frame.");
            TestAssert.Equal<Bitmap>(null, frame, "Empty frame store should not return a bitmap.");
        }

        public static byte[] CreateImageBytes(int width, int height)
        {
            using (var bitmap = new Bitmap(width, height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (var stream = new MemoryStream())
            {
                graphics.Clear(Color.CornflowerBlue);
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
}
