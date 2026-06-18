using PlayPane.Core.Models;
using PlayPane.Core.Windowing;

namespace PlayPane.Tests
{
    internal static class WindowClassifierTests
    {
        public static void ClassifiesSupportedBrowsers()
        {
            TestAssert.Equal(BrowserType.Chrome, WindowClassifier.ClassifyBrowser("chrome", "Guide"), "chrome should be supported.");
            TestAssert.Equal(BrowserType.Firefox, WindowClassifier.ClassifyBrowser("firefox", "Guide"), "firefox should be supported.");
            TestAssert.Equal(BrowserType.Edge, WindowClassifier.ClassifyBrowser("msedge", "Guide"), "msedge should be supported.");
            TestAssert.Equal(BrowserType.Other, WindowClassifier.ClassifyBrowser("notepad", "notes"), "other apps should be other.");
        }

        public static void FiltersDefaultWindowList()
        {
            var chrome = new SourceWindowInfo(100, "Guide", "chrome", BrowserType.Chrome, new WindowBounds(0, 0, 1200, 900), false, true, "DISPLAY1");
            var notepad = new SourceWindowInfo(101, "Notes", "notepad", BrowserType.Other, new WindowBounds(0, 0, 1200, 900), false, true, "DISPLAY1");
            var tiny = new SourceWindowInfo(102, "Tiny", "chrome", BrowserType.Chrome, new WindowBounds(0, 0, 20, 20), false, true, "DISPLAY1");
            var playPane = new SourceWindowInfo(103, "PlayPane", "PlayPane", BrowserType.Other, new WindowBounds(0, 0, 1200, 900), false, true, "DISPLAY1");

            TestAssert.True(WindowClassifier.ShouldInclude(chrome, false), "Supported browsers should be included by default.");
            TestAssert.False(WindowClassifier.ShouldInclude(notepad, false), "Other apps should be hidden by default.");
            TestAssert.True(WindowClassifier.ShouldInclude(notepad, true), "Show all should include other apps.");
            TestAssert.False(WindowClassifier.ShouldInclude(tiny, true), "Tiny windows should be excluded.");
            TestAssert.False(WindowClassifier.ShouldInclude(playPane, true), "PlayPane windows should be excluded.");
        }
    }
}
