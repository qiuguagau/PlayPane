using System;
using System.IO;
using System.Linq;

namespace PlayPane.Tests
{
    internal static class TrayOverlayMenuTests
    {
        public static void TrayOverlayItemsUseDedicatedCommands()
        {
            string source = File.ReadAllText(FindSourceFile("src", "PlayPane", "MainWindow.xaml.cs"));

            TestAssert.True(source.Contains("_showOverlayMenuItem = CreateTrayMenuItem(L(\"Tray.ShowOverlay\"), ShowOverlayFromTray);"), "Tray show overlay item should call the dedicated show command.");
            TestAssert.True(source.Contains("_hideOverlayMenuItem = CreateTrayMenuItem(L(\"Tray.HideOverlay\"), HideOverlayFromTray);"), "Tray hide overlay item should call the dedicated hide command.");
            TestAssert.True(source.Contains("private void ShowOverlayFromTray()"), "Main window should have a dedicated tray show overlay command.");
            TestAssert.True(source.Contains("private void HideOverlayFromTray()"), "Main window should have a dedicated tray hide overlay command.");
        }

        public static void TrayShowOverlayBringsExistingOverlayForward()
        {
            string source = File.ReadAllText(FindSourceFile("src", "PlayPane", "MainWindow.xaml.cs"));
            string method = ExtractMethod(source, "private void ShowExistingOverlayFromTray()");

            TestAssert.True(method.Contains("_overlayWindow.Show();"), "Tray show should show an existing overlay window.");
            TestAssert.True(method.Contains("WindowState.Normal"), "Tray show should restore a minimized overlay before bringing it forward.");
            TestAssert.True(method.Contains("_overlayWindow.Topmost = true;"), "Tray show should briefly make the overlay topmost so it is not hidden behind other windows.");
            TestAssert.True(method.Contains("_overlayWindow.Activate();"), "Tray show should activate an editable overlay.");
            TestAssert.True(method.Contains("_overlayWindow.EnterGameMode();"), "Tray show should preserve game mode behavior for locked overlays.");
        }

        public static void TrayHideOverlayUpdatesState()
        {
            string source = File.ReadAllText(FindSourceFile("src", "PlayPane", "MainWindow.xaml.cs"));
            string method = ExtractMethod(source, "private void HideOverlayFromTray()");

            TestAssert.True(method.Contains("_overlayWindow.Hide();"), "Tray hide should hide the existing overlay.");
            TestAssert.True(method.Contains("UpdateStatus(L(\"Status.OverlayHidden\"));"), "Tray hide should report the hidden overlay state.");
            TestAssert.True(method.Contains("UpdateTrayOverlayMenuState();"), "Tray hide should refresh tray item availability.");
        }

        public static void TrayModeItemsUseDedicatedCommands()
        {
            string source = File.ReadAllText(FindSourceFile("src", "PlayPane", "MainWindow.xaml.cs"));

            TestAssert.True(source.Contains("_trayMenu.Items.Add(CreateTrayMenuItem(L(\"Tray.EnterEditMode\"), EnterEditModeFromTray));"), "Tray enter edit item should call the dedicated edit-mode command.");
            TestAssert.True(source.Contains("_trayMenu.Items.Add(CreateTrayMenuItem(L(\"Tray.EnterGameMode\"), EnterGameModeFromTray));"), "Tray enter game item should call the dedicated game-mode command.");
        }

        public static void TrayEnterEditModeBringsLockedOverlayForward()
        {
            string source = File.ReadAllText(FindSourceFile("src", "PlayPane", "MainWindow.xaml.cs"));
            string method = ExtractMethod(source, "private void EnterEditModeFromTray()");

            TestAssert.True(method.Contains("_overlayWindow.Show();"), "Tray edit mode should show a hidden overlay.");
            TestAssert.True(method.Contains("WindowState.Normal"), "Tray edit mode should restore a minimized overlay.");
            TestAssert.True(method.Contains("_overlayWindow.EnterEditMode();"), "Tray edit mode should switch the overlay out of game mode.");
            TestAssert.True(method.Contains("BringOverlayToForegroundForEditing(_overlayWindow);"), "Tray edit mode should use the reliable foreground helper.");
            TestAssert.True(method.Contains("Dispatcher.BeginInvoke(new Action(delegate"), "Tray edit mode should retry foreground promotion after the tray menu closes.");
            TestAssert.True(method.Contains("UpdateTrayOverlayMenuState();"), "Tray edit mode should refresh tray item availability.");
        }

        public static void TrayEditForegroundHelperUsesWin32ZOrder()
        {
            string source = File.ReadAllText(FindSourceFile("src", "PlayPane", "MainWindow.xaml.cs"));
            string method = ExtractMethod(source, "private void BringOverlayToForegroundForEditing(OverlayWindow overlay)");

            TestAssert.True(method.Contains("Win32Api.SetWindowPos(handle, Win32Api.HWND_TOPMOST"), "Foreground helper should promote the overlay with Win32 topmost z-order.");
            TestAssert.True(method.Contains("Win32Api.SetForegroundWindow(handle);"), "Foreground helper should request foreground activation through Win32.");
            TestAssert.True(method.Contains("overlay.Activate();"), "Foreground helper should still activate the WPF window.");
            TestAssert.True(method.Contains("Win32Api.SetWindowPos(handle, Win32Api.HWND_NOTOPMOST"), "Foreground helper should return the overlay to the edit-mode non-topmost policy.");
        }

        public static void Win32ApiExposesForegroundZOrderPrimitives()
        {
            string source = File.ReadAllText(FindSourceFile("src", "PlayPane.Core", "Native", "Win32Api.cs"));

            TestAssert.True(source.Contains("HWND_TOPMOST"), "Win32 API wrapper should expose HWND_TOPMOST.");
            TestAssert.True(source.Contains("HWND_NOTOPMOST"), "Win32 API wrapper should expose HWND_NOTOPMOST.");
            TestAssert.True(source.Contains("SWP_SHOWWINDOW"), "Win32 API wrapper should expose SWP_SHOWWINDOW.");
            TestAssert.True(source.Contains("SetForegroundWindow"), "Win32 API wrapper should expose SetForegroundWindow.");
        }

        public static void TrayMenuUsesNotifyIconNativeContextMenu()
        {
            string source = File.ReadAllText(FindSourceFile("src", "PlayPane", "MainWindow.xaml.cs"));

            TestAssert.True(source.Contains("_trayIcon.ContextMenuStrip = _trayMenu;"), "Tray menu should be attached to NotifyIcon.ContextMenuStrip for native shell popup behavior.");
            TestAssert.True(source.Contains("_trayMenu.Opening += TrayMenu_Opening;"), "Tray menu should refresh state through the opening event.");
            TestAssert.False(source.Contains("_trayIcon.MouseUp += TrayIcon_MouseUp;"), "Tray menu should not be shown from NotifyIcon.MouseUp.");
            TestAssert.False(source.Contains("_trayMenu.Show(Forms.Control.MousePosition);"), "Tray menu should not be manually shown at the mouse position.");
        }

        public static void TrayMenuOpeningRefreshesMenuState()
        {
            string source = File.ReadAllText(FindSourceFile("src", "PlayPane", "MainWindow.xaml.cs"));
            string method = ExtractMethod(source, "private void TrayMenu_Opening(object sender, CancelEventArgs e)");

            TestAssert.True(method.Contains("UpdateTrayOverlayMenuState();"), "Tray menu should refresh overlay command availability just before opening.");
        }

        private static string ExtractMethod(string source, string signature)
        {
            int start = source.IndexOf(signature, StringComparison.Ordinal);
            TestAssert.True(start >= 0, "Could not find method " + signature);

            int braceStart = source.IndexOf('{', start);
            TestAssert.True(braceStart >= 0, "Could not find method body for " + signature);

            int depth = 0;
            for (int index = braceStart; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(start, index - start + 1);
                    }
                }
            }

            throw new InvalidOperationException("Could not extract method " + signature);
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
