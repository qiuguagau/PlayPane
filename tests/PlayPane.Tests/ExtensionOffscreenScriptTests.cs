using System.IO;

namespace PlayPane.Tests
{
    internal static class ExtensionOffscreenScriptTests
    {
        public static void DesktopExitSignalStopsTabCapture()
        {
            string script = File.ReadAllText(FindExtensionFile("offscreen.js"));

            TestAssert.True(script.Contains("message.type === \"desktop-exiting\""), "Offscreen capture script should handle the desktop exit signal.");
            TestAssert.True(script.Contains("stopCapture();\n    reportStatus(\"idle\", \"\");"), "Desktop exit should fully stop tab capture and report an idle state.");
        }

        public static void OffscreenCaptureDoesNotPlayDuplicateHiddenVideo()
        {
            string script = File.ReadAllText(FindExtensionFile("offscreen.js"));

            TestAssert.True(!script.Contains("document.createElement(\"video\")"), "Offscreen capture should not render a duplicate hidden playback stream.");
            TestAssert.True(!script.Contains("video.play()"), "Offscreen capture should send the stream to WebRTC without extra local playback.");
        }

        public static void OffscreenCaptureEnforcesRequestedFrameRateAtCaptureAndSender()
        {
            string script = File.ReadAllText(FindExtensionFile("offscreen.js"));
            string background = File.ReadAllText(FindExtensionFile("background.js"));

            TestAssert.True(background.Contains("frameRate: 30"), "Extension capture should default to the desktop app's standard 30 FPS mode instead of starting at 60 FPS.");
            TestAssert.True(script.Contains("maxFrameRate: captureOptions.frameRate"), "Initial tab capture should request the configured frame-rate limit.");
            TestAssert.True(script.Contains("sender.setParameters(parameters)"), "WebRTC sender parameters should enforce frame-rate limits after negotiation starts.");
            TestAssert.True(script.Contains("maxFramerate = captureOptions.frameRate"), "WebRTC sender encoding should use the requested frame-rate cap.");
        }

        private static string FindExtensionFile(string fileName)
        {
            DirectoryInfo directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "extensions", "chromium-playpane", fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException("Could not find " + fileName + " from the current test directory.");
        }
    }
}
