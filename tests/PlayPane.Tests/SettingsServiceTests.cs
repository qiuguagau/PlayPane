using System.IO;
using PlayPane.Core.Models;
using PlayPane.Core.Services;

namespace PlayPane.Tests
{
    internal static class SettingsServiceTests
    {
        public static void SaveAndLoadRoundTrip()
        {
            var directory = Path.Combine(Path.GetTempPath(), "PlayPane.Tests", Path.GetRandomFileName());
            var path = Path.Combine(directory, "settings.json");
            var service = new SettingsService(path);

            var settings = AppSettings.CreateDefault();
            settings.OpacityPercent = 55;
            settings.FrameRateMode = FrameRateMode.Smooth;
            settings.Language = AppLanguage.SimplifiedChinese;
            settings.CaptureSourceKind = CaptureSourceKind.BrowserExtension;
            settings.CropRegion = CropRegion.FromPixels(10, 20, 200, 100, 1000, 500);
            settings.OverlayBounds = new WindowBounds(11, 22, 333, 222);
            settings.PreviousSource = new PreviousSourceWindow("chrome", BrowserType.Chrome, "Guide", new WindowBounds(1, 2, 3, 4));

            service.Save(settings);
            var loaded = service.Load();

            TestAssert.Equal(55, loaded.OpacityPercent, "Opacity should round-trip.");
            TestAssert.Equal(FrameRateMode.Smooth, loaded.FrameRateMode, "Frame rate should round-trip.");
            TestAssert.Equal(AppLanguage.SimplifiedChinese, loaded.Language, "Language should round-trip.");
            TestAssert.Equal(CaptureSourceKind.BrowserExtension, loaded.CaptureSourceKind, "Capture source should round-trip.");
            TestAssert.Equal(11, loaded.OverlayBounds.X, "Overlay x should round-trip.");
            TestAssert.Equal(BrowserType.Chrome, loaded.PreviousSource.BrowserType, "Previous browser should round-trip.");
            TestAssert.Near(settings.CropRegion.X, loaded.CropRegion.X, 0.0001, "Crop x should round-trip.");

            Directory.Delete(directory, true);
        }

        public static void LanguageRoundTrips()
        {
            var directory = Path.Combine(Path.GetTempPath(), "PlayPane.Tests", Path.GetRandomFileName());
            var path = Path.Combine(directory, "settings.json");
            var service = new SettingsService(path);

            var settings = AppSettings.CreateDefault();
            settings.Language = AppLanguage.SimplifiedChinese;

            service.Save(settings);
            var loaded = service.Load();

            TestAssert.Equal(AppLanguage.SimplifiedChinese, loaded.Language, "Saved language should be loaded.");

            Directory.Delete(directory, true);
        }

        public static void DefaultsToBrowserExtensionCapture()
        {
            var settings = AppSettings.CreateDefault();

            TestAssert.Equal(CaptureSourceKind.BrowserExtension, settings.CaptureSourceKind, "PlayPane should default to the Chrome/Edge extension capture route.");
        }
    }
}
