using System.IO;
using PlayPane.Core.Application;
using PlayPane.Core.Models;
using PlayPane.Core.Services;
using PlayPane.Core.Windowing;

namespace PlayPane.Tests
{
    internal static class SessionManagerTests
    {
        public static void StopBrowserExtensionSessionIsIdempotent()
        {
            string directory = Path.Combine(Path.GetTempPath(), "PlayPane.Tests", Path.GetRandomFileName());
            var stateManager = new StateManager();
            var recoveryService = new RecoveryService(Path.Combine(directory, "recovery.json"));
            var settingsService = new SettingsService(Path.Combine(directory, "settings.json"));
            var sessionManager = new SessionManager(stateManager, new SourceWindowManager(), recoveryService, settingsService);

            sessionManager.StartBrowserExtension(AppSettings.CreateDefault());
            sessionManager.Stop(true);
            sessionManager.Stop(true);

            TestAssert.Equal<PlayPane.Core.Models.CaptureSession>(null, sessionManager.CurrentSession, "Stop should leave no active session.");
            TestAssert.Equal(PlayPaneState.Preview, stateManager.Current, "Stop should leave the app ready to preview/select again.");

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
