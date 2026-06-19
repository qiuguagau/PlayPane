using PlayPane.Core.Models;

namespace PlayPane.Tests
{
    internal static class CaptureSessionTests
    {
        public static void BrowserExtensionSessionDoesNotRequireSourceWindow()
        {
            var settings = AppSettings.CreateDefault();

            CaptureSession session = CaptureSession.ForBrowserExtension(settings);

            TestAssert.Equal(CaptureSourceKind.BrowserExtension, session.SourceKind, "Extension session should be marked as extension capture.");
            TestAssert.Equal<SourceWindowInfo>(null, session.Source, "Extension session should not require a native source window.");
        }
    }
}
