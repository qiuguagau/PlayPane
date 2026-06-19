using System.IO;

namespace PlayPane.Tests
{
    internal static class WebView2RuntimeTests
    {
        public static void WebRtcViewersUseRuntimeConfiguredForVisibleOverlayPlayback()
        {
            string runtimeCode = File.ReadAllText(FindSourceFile("src", "PlayPane", "WebView2Runtime.cs"));
            string overlayCode = File.ReadAllText(FindSourceFile("src", "PlayPane", "OverlayWindow.xaml.cs"));
            string mainCode = File.ReadAllText(FindSourceFile("src", "PlayPane", "MainWindow.xaml.cs"));

            TestAssert.True(runtimeCode.Contains("--disable-renderer-backgrounding"), "WebView2 should not throttle the visible overlay renderer while a game has focus.");
            TestAssert.True(runtimeCode.Contains("--disable-backgrounding-occluded-windows"), "WebView2 should not treat the game-focused overlay as an occluded background window.");
            TestAssert.True(runtimeCode.Contains("--disable-background-timer-throttling"), "WebView2 signaling and playback timers should remain responsive during game mode.");
            TestAssert.True(overlayCode.Contains("EnsureCoreWebView2Async(await WebView2Runtime.GetEnvironmentAsync()"), "Overlay viewer should use the shared low-throttling WebView2 runtime.");
            TestAssert.True(mainCode.Contains("EnsureCoreWebView2Async(await WebView2Runtime.GetEnvironmentAsync()"), "Preview viewer should use the same WebView2 runtime as the overlay.");
        }

        private static string FindSourceFile(params string[] relativeParts)
        {
            DirectoryInfo directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, Path.Combine(relativeParts));
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException("Could not find " + Path.Combine(relativeParts) + " from the current test directory.");
        }
    }
}
