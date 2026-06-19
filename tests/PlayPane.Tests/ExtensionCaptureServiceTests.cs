using System;
using System.Drawing;
using PlayPane.Core.Capture;
using PlayPane.Core.Models;

namespace PlayPane.Tests
{
    internal static class ExtensionCaptureServiceTests
    {
        public static void ThrowsWhenNoExtensionFrameExists()
        {
            var service = new ExtensionCaptureService(new ExtensionFrameStore());

            try
            {
                service.Capture(null, CaptureMode.FullWindow, CropRegion.Full);
                throw new InvalidOperationException("Expected capture to fail without extension frames.");
            }
            catch (WindowCaptureException ex)
            {
                TestAssert.True(ex.Message.Contains("extension"), "Error should mention extension frames.");
            }
        }

        public static void ReturnsLatestExtensionFrame()
        {
            var store = new ExtensionFrameStore();
            store.Update(ExtensionFrameStoreTests.CreateImageBytes(40, 20));
            var service = new ExtensionCaptureService(store);

            using (Bitmap frame = service.Capture(null, CaptureMode.FullWindow, CropRegion.Full))
            {
                TestAssert.Equal(40, frame.Width, "Extension capture should return latest frame width.");
                TestAssert.Equal(20, frame.Height, "Extension capture should return latest frame height.");
            }
        }
    }
}
