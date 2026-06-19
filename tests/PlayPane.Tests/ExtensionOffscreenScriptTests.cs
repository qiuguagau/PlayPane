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
