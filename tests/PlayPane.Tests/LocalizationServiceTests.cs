using PlayPane.Core.Models;
using PlayPane.Core.Services;

namespace PlayPane.Tests
{
    internal static class LocalizationServiceTests
    {
        public static void UsesEnglishByDefault()
        {
            var localizer = new LocalizationService(AppLanguage.English);

            TestAssert.Equal("Settings", localizer.Get("Main.Settings"), "English settings label should be available.");
        }

        public static void TranslatesSimplifiedChinese()
        {
            var localizer = new LocalizationService(AppLanguage.SimplifiedChinese);

            TestAssert.Equal("设置", localizer.Get("Main.Settings"), "Chinese settings label should be available.");
            TestAssert.Equal("开始覆盖层", localizer.Get("Main.StartOverlay"), "Chinese start action should be available.");
        }

        public static void FallsBackToEnglishForUnknownKey()
        {
            var localizer = new LocalizationService(AppLanguage.SimplifiedChinese);

            TestAssert.Equal("Missing.Key", localizer.Get("Missing.Key"), "Unknown keys should fall back to the key name.");
        }
    }
}
