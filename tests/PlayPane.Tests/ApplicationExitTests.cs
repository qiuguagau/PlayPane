using System;
using System.IO;
using System.Linq;

namespace PlayPane.Tests
{
    internal static class ApplicationExitTests
    {
        public static void ExitRequestsExtensionCaptureStopBeforeStoppingServer()
        {
            string source = File.ReadAllText(FindSourceFile("src", "PlayPane", "MainWindow.xaml.cs"));
            int shutdownIndex = source.IndexOf("CompleteExitShutdownAsync", StringComparison.Ordinal);

            TestAssert.True(shutdownIndex >= 0, "Main window should have a dedicated exit shutdown flow.");

            int requestIndex = source.IndexOf("RequestSourceCaptureStopAsync", shutdownIndex, StringComparison.Ordinal);
            int stopServerIndex = source.IndexOf("StopExtensionServerAsync().ConfigureAwait", shutdownIndex, StringComparison.Ordinal);

            TestAssert.True(requestIndex >= 0, "Main window exit flow should request the browser extension to stop capture.");
            TestAssert.True(stopServerIndex >= 0, "Main window exit flow should stop the extension signaling server.");
            TestAssert.True(requestIndex < stopServerIndex, "Extension capture should be stopped before the signaling server is shut down.");
        }

        private static string FindSourceFile(params string[] relativeParts)
        {
            DirectoryInfo directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null)
            {
                string candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
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
