using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using PlayPane.Core.Capture;
using PlayPane.Core.Input;
using PlayPane.Core.Models;
using PlayPane.Core.Native;
using PlayPane.Core.Services;

namespace PlayPane
{
    public partial class MainWindow : Window
    {
        private readonly SettingsService _settingsService = new SettingsService();
        private readonly ExtensionSignalingServer _extensionSignalingServer = new ExtensionSignalingServer();
        private readonly GlobalHotkeyService _hotkeyService = new GlobalHotkeyService();
        private readonly FrameRateController _frameRateController = new FrameRateController();
        private AppSettings _settings;
        private OverlayWindow _overlayWindow;
        private Forms.NotifyIcon _trayIcon;
        private Forms.ContextMenuStrip _trayMenu;
        private Forms.ToolStripMenuItem _showOverlayMenuItem;
        private Forms.ToolStripMenuItem _hideOverlayMenuItem;
        private LocalizationService _localizer;
        private bool _explicitExit;
        private bool _exitShutdownComplete;
        private bool _exitShutdownInProgress;
        private bool _isStoppingMirroring;
        private bool _isPreviewViewerActive;
        private Task _pendingStopTask = Task.CompletedTask;

        public MainWindow()
        {
            InitializeComponent();
            _extensionSignalingServer.ClientConnected += ExtensionServer_ClientConnected;
            _extensionSignalingServer.ClientDisconnected += ExtensionServer_ClientDisconnected;
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            Closed += MainWindow_Closed;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source != null)
            {
                source.AddHook(WndProc);
            }

            RegisterHotkeys();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = _hotkeyService.ProcessWindowMessage(msg, wParam);
            return IntPtr.Zero;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _settings = _settingsService.Load();
            _settings.EnsureValid();
            _settings.CaptureSourceKind = CaptureSourceKind.BrowserExtension;
            _settings.CaptureMode = CaptureMode.FullWindow;
            _localizer = new LocalizationService(_settings.Language);

            ApplyLocalization();
            ApplySettingsToUi();
            CreateTrayIcon();
            await StartExtensionServerAsync().ConfigureAwait(true);
            await StartPreviewViewerAsync().ConfigureAwait(true);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_explicitExit)
            {
                e.Cancel = true;
                Hide();
                UpdateStatus(L("Status.ControlPanelMinimized"));
                return;
            }

            if (!_exitShutdownComplete)
            {
                e.Cancel = true;
                BeginExitShutdown();
                return;
            }

            SaveSettingsFromUi();
            StopMirroring(false);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _hotkeyService.Dispose();
            if (!_exitShutdownComplete)
            {
                _ = StopPreviewViewerAsync();
                _ = StopExtensionServerAsync();
            }
            DisposeTrayIcon();
        }

        private void RegisterHotkeys()
        {
            if (_settings == null)
            {
                _settings = _settingsService.Load();
                _settings.EnsureValid();
            }

            if (_localizer == null)
            {
                _localizer = new LocalizationService(_settings.Language);
            }

            _hotkeyService.HotkeyPressed -= HotkeyService_HotkeyPressed;
            _hotkeyService.HotkeyPressed += HotkeyService_HotkeyPressed;
            IReadOnlyList<HotkeyAction> failed = _hotkeyService.Register(new WindowInteropHelper(this).Handle, _settings.Shortcuts);
            if (failed.Count > 0)
            {
                UpdateStatus(L("Status.HotkeysFailed"));
            }
        }

        private void HotkeyService_HotkeyPressed(object sender, HotkeyPressedEventArgs e)
        {
            if (e.Action == HotkeyAction.ToggleOverlay)
            {
                ToggleOverlayVisibility();
            }
            else if (e.Action == HotkeyAction.ToggleEditGameMode)
            {
                ToggleOverlayMode();
            }
            else if (e.Action == HotkeyAction.IncreaseOpacity)
            {
                AdjustOpacity(5);
            }
            else if (e.Action == HotkeyAction.DecreaseOpacity)
            {
                AdjustOpacity(-5);
            }
            else if (e.Action == HotkeyAction.StopMirroring)
            {
                StopMirroring(true);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void StartOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            StartOverlay();
        }

        private void StopMirroringButton_Click(object sender, RoutedEventArgs e)
        {
            StopMirroring(true);
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityValueTextBlock != null)
            {
                OpacityValueTextBlock.Text = ((int)e.NewValue) + "%";
            }

            if (_settings != null)
            {
                _settings.OpacityPercent = (int)e.NewValue;
            }

            if (_overlayWindow != null)
            {
                _overlayWindow.SetOpacityPercent((int)e.NewValue);
            }
        }

        private void AspectRatioCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || _settings == null)
            {
                return;
            }

            _settings.AspectRatioLocked = AspectRatioCheckBox.IsChecked == true;
            _settingsService.Save(_settings);
            RestartPreviewViewerIfActive();
        }

        private void FrameRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _settings == null)
            {
                return;
            }

            _settings.FrameRateMode = ReadFrameRateMode(FrameRateComboBox);
            _settingsService.Save(_settings);
            RestartPreviewViewerIfActive();
        }

        private async void StartOverlay()
        {
            SaveSettingsFromUi();
            if (!await StartExtensionServerAsync().ConfigureAwait(true))
            {
                return;
            }

            await StopPreviewViewerAsync().ConfigureAwait(true);

            CaptureSession session = CaptureSession.ForBrowserExtension(_settings);
            if (_overlayWindow == null)
            {
                _overlayWindow = new OverlayWindow(_settingsService);
                _overlayWindow.StopRequested += OverlayWindow_StopRequested;
                _overlayWindow.HiddenRequested += OverlayWindow_HiddenRequested;
                _overlayWindow.Closed += OverlayWindow_Closed;
            }

            _overlayWindow.Configure(session, _settings, _extensionSignalingServer.WebSocketUri);
            _overlayWindow.Show();
            _overlayWindow.Activate();
            _overlayWindow.EnterEditMode();
            Hide();
            UpdateExtensionStatus();
            UpdateStatus(L("Status.OverlayStarted"));
            UpdateTrayOverlayMenuState();
        }

        private void StopMirroring(bool restartPreview)
        {
            if (_isStoppingMirroring)
            {
                return;
            }

            _isStoppingMirroring = true;
            try
            {
                OverlayWindow overlay = _overlayWindow;
                _overlayWindow = null;
                if (overlay != null)
                {
                    DetachOverlayEvents(overlay);
                    _settings.OverlayBounds = new WindowBounds((int)overlay.Left, (int)overlay.Top, (int)overlay.Width, (int)overlay.Height);
                    overlay.StopCapture();
                    overlay.Close();
                }

                SaveSettingsFromUi();
                UpdateExtensionStatus();
                UpdateStatus(L("Status.MirroringStopped"));
                UpdateTrayOverlayMenuState();
            }
            finally
            {
                _isStoppingMirroring = false;
            }

            if (restartPreview && !_explicitExit)
            {
                _ = StartPreviewViewerAsync();
            }
        }

        private async Task<bool> StartExtensionServerAsync()
        {
            await _pendingStopTask.ConfigureAwait(true);

            try
            {
                await _extensionSignalingServer.StartAsync().ConfigureAwait(true);
                UpdateExtensionStatus();
                UpdateStatus(_localizer.Format("Main.ExtensionServerReady", _extensionSignalingServer.WebSocketUri));
                return true;
            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message);
                return false;
            }
        }

        private async Task StopExtensionServerAsync()
        {
            await _pendingStopTask.ConfigureAwait(true);
            if (!_extensionSignalingServer.IsRunning)
            {
                return;
            }

            _pendingStopTask = Task.Run(async delegate
            {
                try
                {
                    await _extensionSignalingServer.StopAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Shutdown is best effort while the WPF dispatcher is closing.
                }
            });
            await _pendingStopTask.ConfigureAwait(true);
        }

        private void BeginExitShutdown()
        {
            if (_exitShutdownInProgress)
            {
                return;
            }

            _exitShutdownInProgress = true;
            _ = CompleteExitShutdownAsync();
        }

        private async Task CompleteExitShutdownAsync()
        {
            try
            {
                SaveSettingsFromUi();
                StopMirroring(false);
                await StopPreviewViewerAsync().ConfigureAwait(true);
                await _extensionSignalingServer.RequestSourceCaptureStopAsync().ConfigureAwait(true);
                await StopExtensionServerAsync().ConfigureAwait(true);
            }
            finally
            {
                _exitShutdownComplete = true;
                Close();
            }
        }

        private async Task StartPreviewViewerAsync()
        {
            if (_overlayWindow != null || _explicitExit)
            {
                return;
            }

            if (!await StartExtensionServerAsync().ConfigureAwait(true))
            {
                PreviewStatusTextBlock.Text = L("Main.PreviewPaused");
                return;
            }

            try
            {
                await PreviewWebView.EnsureCoreWebView2Async(await WebView2Runtime.GetEnvironmentAsync().ConfigureAwait(true)).ConfigureAwait(true);
                PreviewWebView.DefaultBackgroundColor = Drawing.Color.FromArgb(2, 6, 23);
                PreviewWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                PreviewWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                PreviewWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                PreviewWebView.CoreWebView2.NavigateToString(WebRtcViewerPage.Build(
                    _extensionSignalingServer.WebSocketUri,
                    L("Overlay.WebRtcWaiting"),
                    _settings.AspectRatioLocked,
                    100,
                    _frameRateController.GetFramesPerSecond(_settings.FrameRateMode)));
                _isPreviewViewerActive = true;
                PreviewStatusTextBlock.Text = L("Main.PreviewWaiting");
                UpdateExtensionStatus();
            }
            catch (Exception ex)
            {
                _isPreviewViewerActive = false;
                PreviewStatusTextBlock.Text = L("Main.PreviewPaused");
                UpdateStatus(ex.Message);
            }
        }

        private async Task StopPreviewViewerAsync()
        {
            try
            {
                if (PreviewWebView.CoreWebView2 != null)
                {
                    await PreviewWebView.CoreWebView2.ExecuteScriptAsync("window.playPaneStopViewer && window.playPaneStopViewer();").ConfigureAwait(true);
                    await Task.Delay(80).ConfigureAwait(true);
                    PreviewWebView.CoreWebView2.NavigateToString(WebRtcViewerPage.Blank);
                }
            }
            catch
            {
                // The WebView may be closing or not fully initialized.
            }
            finally
            {
                _isPreviewViewerActive = false;
                if (PreviewStatusTextBlock != null)
                {
                    PreviewStatusTextBlock.Text = L("Main.PreviewPaused");
                }
            }
        }

        private async void RestartPreviewViewerIfActive()
        {
            if (!_isPreviewViewerActive || _overlayWindow != null)
            {
                return;
            }

            await StopPreviewViewerAsync().ConfigureAwait(true);
            await StartPreviewViewerAsync().ConfigureAwait(true);
        }

        private void ExtensionServer_ClientConnected(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                UpdateExtensionStatus();
                UpdateStatus(L("Status.ExtensionClientConnected"));
            }));
        }

        private void ExtensionServer_ClientDisconnected(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                UpdateExtensionStatus();
                UpdateStatus(L("Status.ExtensionClientDisconnected"));
            }));
        }

        private void ApplySettingsToUi()
        {
            OpacitySlider.Value = _settings.OpacityPercent;
            AspectRatioCheckBox.IsChecked = _settings.AspectRatioLocked;
            SelectComboItem(FrameRateComboBox, _settings.FrameRateMode.ToString());
            UpdateExtensionStatus();
        }

        private void SaveSettingsFromUi()
        {
            if (_settings == null)
            {
                _settings = AppSettings.CreateDefault();
            }

            _settings.OpacityPercent = (int)OpacitySlider.Value;
            _settings.AspectRatioLocked = AspectRatioCheckBox.IsChecked == true;
            _settings.FrameRateMode = ReadFrameRateMode(FrameRateComboBox);
            _settings.CaptureSourceKind = CaptureSourceKind.BrowserExtension;
            _settings.CaptureMode = CaptureMode.FullWindow;
            _settings.Language = _localizer == null ? _settings.Language : _localizer.Language;

            if (_overlayWindow != null)
            {
                _settings.OverlayBounds = new WindowBounds((int)_overlayWindow.Left, (int)_overlayWindow.Top, (int)_overlayWindow.Width, (int)_overlayWindow.Height);
            }

            _settingsService.Save(_settings);
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow(_settings, _localizer);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                _settings.StartWithWindows = settingsWindow.StartWithWindows;
                _settings.Language = settingsWindow.SelectedLanguage;
                _settings.CaptureSourceKind = CaptureSourceKind.BrowserExtension;
                _localizer = new LocalizationService(_settings.Language);
                _settingsService.Save(_settings);
                RegisterHotkeys();
                ApplyLocalization();
                RecreateTrayIcon();
                if (_overlayWindow != null)
                {
                    _overlayWindow.SetLanguage(_settings.Language);
                }

                RestartPreviewViewerIfActive();
                UpdateStatus(L("Status.SettingsSaved"));
            }
        }

        private static FrameRateMode ReadFrameRateMode(ComboBox comboBox)
        {
            string tag = ReadSelectedTag(comboBox);
            FrameRateMode value;
            if (Enum.TryParse(tag, out value))
            {
                return value;
            }

            return FrameRateMode.Standard;
        }

        private static string ReadSelectedTag(ComboBox comboBox)
        {
            ComboBoxItem item = comboBox.SelectedItem as ComboBoxItem;
            return item == null || item.Tag == null ? string.Empty : item.Tag.ToString();
        }

        private static void SelectComboItem(ComboBox comboBox, string tag)
        {
            foreach (object itemObject in comboBox.Items)
            {
                ComboBoxItem item = itemObject as ComboBoxItem;
                if (item != null && item.Tag != null && item.Tag.ToString() == tag)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private void ToggleOverlayVisibility()
        {
            if (_overlayWindow == null || !_overlayWindow.IsVisible)
            {
                ShowOverlayFromTray();
                return;
            }

            HideOverlayFromTray();
        }

        private void ToggleOverlayMode()
        {
            if (_overlayWindow == null)
            {
                return;
            }

            if (_overlayWindow.IsGameMode)
            {
                _overlayWindow.EnterEditMode();
            }
            else
            {
                _overlayWindow.EnterGameMode();
            }
        }

        private void AdjustOpacity(int delta)
        {
            int value = Math.Max(10, Math.Min(100, (int)OpacitySlider.Value + delta));
            OpacitySlider.Value = value;
            if (_overlayWindow != null)
            {
                _overlayWindow.SetOpacityPercent(value);
            }

            SaveSettingsFromUi();
        }

        private void CreateTrayIcon()
        {
            if (_trayIcon != null)
            {
                return;
            }

            _trayIcon = new Forms.NotifyIcon();
            _trayIcon.Text = "PlayPane";
            _trayIcon.Icon = Drawing.SystemIcons.Application;
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate { QueueUiAction(ShowControlPanel); };

            _trayMenu = new Forms.ContextMenuStrip();
            _trayMenu.Opening += TrayMenu_Opening;
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.ShowControlPanel"), ShowControlPanel));
            _showOverlayMenuItem = CreateTrayMenuItem(L("Tray.ShowOverlay"), ShowOverlayFromTray);
            _hideOverlayMenuItem = CreateTrayMenuItem(L("Tray.HideOverlay"), HideOverlayFromTray);
            _trayMenu.Items.Add(_showOverlayMenuItem);
            _trayMenu.Items.Add(_hideOverlayMenuItem);
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.EnterEditMode"), EnterEditModeFromTray));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.EnterGameMode"), EnterGameModeFromTray));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.StopMirroring"), delegate { StopMirroring(true); }));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.Settings"), OpenSettings));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.Exit"), ExitApplication));
            UpdateTrayOverlayMenuState();
            _trayIcon.ContextMenuStrip = _trayMenu;
        }

        private Forms.ToolStripMenuItem CreateTrayMenuItem(string text, Action action)
        {
            var item = new Forms.ToolStripMenuItem(text);
            item.Click += delegate { QueueUiAction(action); };
            return item;
        }

        private void TrayMenu_Opening(object sender, CancelEventArgs e)
        {
            UpdateTrayOverlayMenuState();
        }

        private void ShowOverlayFromTray()
        {
            if (_overlayWindow == null)
            {
                StartOverlay();
                return;
            }

            ShowExistingOverlayFromTray();
            UpdateTrayOverlayMenuState();
        }

        private void ShowExistingOverlayFromTray()
        {
            _overlayWindow.Show();
            if (_overlayWindow.WindowState == WindowState.Minimized)
            {
                _overlayWindow.WindowState = WindowState.Normal;
            }

            if (_overlayWindow.IsGameMode)
            {
                _overlayWindow.EnterGameMode();
                return;
            }

            _overlayWindow.Topmost = true;
            _overlayWindow.Activate();
            _overlayWindow.Topmost = false;
        }

        private void HideOverlayFromTray()
        {
            if (_overlayWindow == null)
            {
                UpdateTrayOverlayMenuState();
                return;
            }

            _overlayWindow.Hide();
            UpdateStatus(L("Status.OverlayHidden"));
            UpdateTrayOverlayMenuState();
        }

        private void EnterEditModeFromTray()
        {
            if (_overlayWindow == null)
            {
                return;
            }

            _overlayWindow.Show();
            if (_overlayWindow.WindowState == WindowState.Minimized)
            {
                _overlayWindow.WindowState = WindowState.Normal;
            }

            _overlayWindow.EnterEditMode();
            BringOverlayToForegroundForEditing(_overlayWindow);
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (_overlayWindow != null && _overlayWindow.IsVisible && !_overlayWindow.IsGameMode)
                {
                    BringOverlayToForegroundForEditing(_overlayWindow);
                }
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            UpdateTrayOverlayMenuState();
        }

        private void BringOverlayToForegroundForEditing(OverlayWindow overlay)
        {
            if (overlay == null)
            {
                return;
            }

            IntPtr handle = new WindowInteropHelper(overlay).Handle;
            uint flags = Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_SHOWWINDOW;
            if (handle != IntPtr.Zero)
            {
                Win32Api.SetWindowPos(handle, Win32Api.HWND_TOPMOST, 0, 0, 0, 0, flags);
                Win32Api.SetForegroundWindow(handle);
            }

            overlay.Activate();

            if (handle != IntPtr.Zero)
            {
                Win32Api.SetWindowPos(handle, Win32Api.HWND_NOTOPMOST, 0, 0, 0, 0, flags);
            }

            overlay.Topmost = false;
        }

        private void EnterGameModeFromTray()
        {
            if (_overlayWindow == null)
            {
                return;
            }

            _overlayWindow.Show();
            if (_overlayWindow.WindowState == WindowState.Minimized)
            {
                _overlayWindow.WindowState = WindowState.Normal;
            }

            _overlayWindow.EnterGameMode();
            UpdateTrayOverlayMenuState();
        }

        private void UpdateTrayOverlayMenuState()
        {
            bool canUseOverlayCommands = !_exitShutdownInProgress;
            bool overlayVisible = _overlayWindow != null && _overlayWindow.IsVisible;

            if (_showOverlayMenuItem != null)
            {
                _showOverlayMenuItem.Enabled = canUseOverlayCommands;
            }

            if (_hideOverlayMenuItem != null)
            {
                _hideOverlayMenuItem.Enabled = canUseOverlayCommands && overlayVisible;
            }
        }

        private void QueueUiAction(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(delegate
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    UpdateStatus(ex.Message);
                }
            }));
        }

        private void RecreateTrayIcon()
        {
            DisposeTrayIcon();
            CreateTrayIcon();
        }

        private void DisposeTrayIcon()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.ContextMenuStrip = null;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_trayMenu != null)
            {
                _trayMenu.Opening -= TrayMenu_Opening;
                _trayMenu.Dispose();
                _trayMenu = null;
                _showOverlayMenuItem = null;
                _hideOverlayMenuItem = null;
            }
        }

        private void ShowControlPanel()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            _explicitExit = true;
            Close();
        }

        private void OverlayWindow_StopRequested(object sender, EventArgs e)
        {
            StopMirroring(true);
            ShowControlPanel();
        }

        private void OverlayWindow_HiddenRequested(object sender, EventArgs e)
        {
            UpdateStatus(L("Status.OverlayHidden"));
            UpdateTrayOverlayMenuState();
        }

        private void OverlayWindow_Closed(object sender, EventArgs e)
        {
            OverlayWindow overlay = sender as OverlayWindow;
            if (overlay != null)
            {
                DetachOverlayEvents(overlay);
                overlay.StopCapture();
            }

            if (_overlayWindow == overlay)
            {
                _overlayWindow = null;
            }

            UpdateTrayOverlayMenuState();

            if (!_isStoppingMirroring && !_explicitExit)
            {
                SaveSettingsFromUi();
                UpdateStatus(L("Status.MirroringStopped"));
                _ = StartPreviewViewerAsync();
            }
        }

        private void DetachOverlayEvents(OverlayWindow overlay)
        {
            overlay.StopRequested -= OverlayWindow_StopRequested;
            overlay.HiddenRequested -= OverlayWindow_HiddenRequested;
            overlay.Closed -= OverlayWindow_Closed;
        }

        private void UpdateExtensionStatus()
        {
            if (ExtensionStatusTextBlock == null)
            {
                return;
            }

            SignalingAddressTextBox.Text = _extensionSignalingServer.WebSocketUri.ToString();
            ExtensionStatusTextBlock.Text = _extensionSignalingServer.IsRunning
                ? _localizer.Format("Main.ExtensionServerReady", _extensionSignalingServer.WebSocketUri)
                : L("Main.ExtensionWaiting");
        }

        private void UpdateStatus(string message)
        {
            if (StatusTextBlock != null)
            {
                StatusTextBlock.Text = message;
            }

            if (_trayIcon != null)
            {
                _trayIcon.Text = message.Length > 63 ? message.Substring(0, 63) : message;
            }
        }

        private string L(string key)
        {
            if (_localizer == null)
            {
                _localizer = new LocalizationService(_settings == null ? AppLanguage.English : _settings.Language);
            }

            return _localizer.Get(key);
        }

        private void ApplyLocalization()
        {
            Title = L("Main.Title");
            SettingsButton.Content = L("Main.Settings");
            SubtitleTextBlock.Text = L("Main.Subtitle");
            PreviewTitleTextBlock.Text = L("Main.SourcePreview");
            PreviewStatusTextBlock.Text = _isPreviewViewerActive ? L("Main.PreviewWaiting") : L("Main.PreviewPaused");
            ExtensionLabelTextBlock.Text = L("Main.ExtensionCaptureSource");
            SignalingAddressLabel.Text = L("Main.SignalingAddress");
            InstructionsLabel.Text = L("Main.Instructions");
            InstructionsTextBlock.Text = L("Main.ExtensionInstructions");
            FrameRateLabel.Text = L("Main.FrameRate");
            FrameRateLowItem.Content = L("Main.FrameRateLow");
            FrameRateStandardItem.Content = L("Main.FrameRateStandard");
            FrameRateSmoothItem.Content = L("Main.FrameRateSmooth");
            OpacityLabel.Text = L("Main.Opacity");
            AspectRatioCheckBox.Content = L("Main.LockAspectRatio");
            StartOverlayButton.Content = L("Main.StartOverlay");
            StopMirroringButton.Content = L("Main.StopMirroring");
            UpdateExtensionStatus();
            UpdateStatus(L("App.Ready"));
        }
    }
}
