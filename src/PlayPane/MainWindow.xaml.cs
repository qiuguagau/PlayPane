using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using PlayPane.Core.Application;
using PlayPane.Core.Capture;
using PlayPane.Core.Input;
using PlayPane.Core.Models;
using PlayPane.Core.Services;
using PlayPane.Core.Windowing;

namespace PlayPane
{
    public partial class MainWindow : Window
    {
        private readonly WindowEnumerator _windowEnumerator = new WindowEnumerator();
        private readonly SettingsService _settingsService = new SettingsService();
        private readonly RecoveryService _recoveryService = new RecoveryService();
        private readonly StateManager _stateManager = new StateManager();
        private readonly SourceWindowManager _sourceWindowManager = new SourceWindowManager();
        private readonly WindowCaptureService _captureService = new WindowCaptureService();
        private readonly ExtensionFrameStore _extensionFrameStore = new ExtensionFrameStore();
        private readonly GlobalHotkeyService _hotkeyService = new GlobalHotkeyService();
        private readonly DispatcherTimer _previewTimer = new DispatcherTimer();
        private readonly List<SourceWindowInfo> _allWindows = new List<SourceWindowInfo>();
        private readonly ExtensionCaptureService _extensionCaptureService;
        private readonly ExtensionFrameServer _extensionFrameServer;
        private SessionManager _sessionManager;
        private AppSettings _settings;
        private OverlayWindow _overlayWindow;
        private Forms.NotifyIcon _trayIcon;
        private Forms.ContextMenuStrip _trayMenu;
        private bool _explicitExit;
        private LocalizationService _localizer;
        private DateTime _lastExtensionStatusUtc = DateTime.MinValue;
        private bool _isStoppingMirroring;
        private Task _pendingStopTask = Task.CompletedTask;

        public MainWindow()
        {
            InitializeComponent();
            _extensionCaptureService = new ExtensionCaptureService(_extensionFrameStore);
            _extensionFrameServer = new ExtensionFrameServer(_extensionFrameStore);
            _extensionFrameServer.ClientConnected += ExtensionFrameServer_ClientConnected;
            _extensionFrameServer.ClientDisconnected += ExtensionFrameServer_ClientDisconnected;
            _extensionFrameServer.FrameReceived += ExtensionFrameServer_FrameReceived;
            _sessionManager = new SessionManager(_stateManager, _sourceWindowManager, _recoveryService, _settingsService);
            _previewTimer.Interval = TimeSpan.FromMilliseconds(1200);
            _previewTimer.Tick += PreviewTimer_Tick;
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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _settings = _settingsService.Load();
            _localizer = new LocalizationService(_settings.Language);
            ApplyLocalization();
            ApplySettingsToUi();
            CreateTrayIcon();
            HandlePendingRecovery();
            RefreshWindowList();

            if (_settings.AutoRestorePreviousSessionOnStartup)
            {
                RestorePreviousSession();
            }
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

            SaveSettingsFromUi();
            StopMirroring(true);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _hotkeyService.Dispose();
            StopExtensionServer();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.MouseUp -= TrayIcon_MouseUp;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_trayMenu != null)
            {
                _trayMenu.Dispose();
                _trayMenu = null;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = _hotkeyService.ProcessWindowMessage(msg, wParam);
            return IntPtr.Zero;
        }

        private void RegisterHotkeys()
        {
            if (_settings == null)
            {
                _settings = _settingsService.Load();
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
            else if (e.Action == HotkeyAction.ReconfigureCrop)
            {
                OpenCropEditor();
            }
            else if (e.Action == HotkeyAction.StopMirroring)
            {
                StopMirroring(true);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshWindowList();
        }

        private void ShowAllWindowsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            RefreshWindowList();
        }

        private void WindowSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyWindowFilter();
        }

        private void CaptureSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            UpdateCaptureSourceUi();
        }

        private void WindowListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SourceWindowInfo selected = GetSelectedSource();
            if (selected == null)
            {
                SelectedSourceTextBlock.Text = string.Empty;
                _previewTimer.Stop();
                return;
            }

            SelectedSourceTextBlock.Text = selected.DisplayName;
            if (_stateManager.Current == PlayPaneState.NoSourceSelected)
            {
                _stateManager.GoTo(PlayPaneState.Preview);
            }

            CapturePreviewFrame();
            _previewTimer.Start();
        }

        private void PreviewTimer_Tick(object sender, EventArgs e)
        {
            CapturePreviewFrame();
        }

        private void StartOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            StartOverlay();
        }

        private void StopMirroringButton_Click(object sender, RoutedEventArgs e)
        {
            StopMirroring(true);
        }

        private void RestoreSourceButton_Click(object sender, RoutedEventArgs e)
        {
            _sessionManager.RestoreSource();
            UpdateStatus(L("Status.SourceRestored"));
        }

        private void RestorePreviousButton_Click(object sender, RoutedEventArgs e)
        {
            RestorePreviousSession();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void EditCropButton_Click(object sender, RoutedEventArgs e)
        {
            OpenCropEditor();
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityValueTextBlock != null)
            {
                OpacityValueTextBlock.Text = ((int)e.NewValue) + "%";
            }

            if (_overlayWindow != null)
            {
                _overlayWindow.SetOpacityPercent((int)e.NewValue);
            }
        }

        private void RefreshWindowList()
        {
            _allWindows.Clear();
            bool showAll = ShowAllWindowsCheckBox.IsChecked == true;
            foreach (SourceWindowInfo window in _windowEnumerator.Enumerate(showAll))
            {
                _allWindows.Add(window);
            }

            ApplyWindowFilter();
            UpdateStatus(_localizer.Format("Status.WindowsFound", _allWindows.Count));
        }

        private void ApplyWindowFilter()
        {
            string query = (WindowSearchTextBox.Text ?? string.Empty).Trim();
            IEnumerable<SourceWindowInfo> filtered = _allWindows;

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(w =>
                    Contains(w.Title, query) ||
                    Contains(w.ProcessName, query) ||
                    Contains(w.BrowserType.ToString(), query));
            }

            WindowListView.ItemsSource = filtered.ToList();
        }

        private async void StartOverlay()
        {
            SaveSettingsFromUi();

            CaptureSession session;
            IWindowCaptureService activeCaptureService;

            if (_settings.CaptureSourceKind == CaptureSourceKind.BrowserExtension)
            {
                if (!await StartExtensionServerAsync())
                {
                    return;
                }

                session = _sessionManager.StartBrowserExtension(_settings);
                activeCaptureService = _extensionCaptureService;
                ExtensionStatusTextBlock.Text = _localizer.Format("Main.ExtensionServerReady", _extensionFrameServer.WebSocketUri);
            }
            else
            {
                SourceWindowInfo source = GetSelectedSource();
                if (source == null)
                {
                    UpdateStatus(L("Status.SelectSourceFirst"));
                    return;
                }

                session = _sessionManager.Start(source, _settings);
                activeCaptureService = _captureService;
            }

            if (_overlayWindow == null)
            {
                _overlayWindow = new OverlayWindow(activeCaptureService, new FrameRateController(), _settingsService);
                _overlayWindow.StopRequested += OverlayWindow_StopRequested;
                _overlayWindow.CropRequested += OverlayWindow_CropRequested;
                _overlayWindow.HiddenRequested += OverlayWindow_HiddenRequested;
                _overlayWindow.Closed += OverlayWindow_Closed;
            }

            _overlayWindow.Configure(session, _settings);
            _overlayWindow.Show();
            _overlayWindow.Activate();
            _overlayWindow.EnterEditMode();
            Hide();
            UpdateStatus(L("Status.OverlayStarted"));
        }

        private void StopMirroring(bool restoreSource)
        {
            if (_isStoppingMirroring)
            {
                return;
            }

            _isStoppingMirroring = true;
            try
            {
                _previewTimer.Stop();

                OverlayWindow overlay = _overlayWindow;
                _overlayWindow = null;
                if (overlay != null)
                {
                    overlay.StopRequested -= OverlayWindow_StopRequested;
                    overlay.CropRequested -= OverlayWindow_CropRequested;
                    overlay.HiddenRequested -= OverlayWindow_HiddenRequested;
                    overlay.Closed -= OverlayWindow_Closed;
                    overlay.StopCapture();
                    overlay.Close();
                }

                _sessionManager.Stop(restoreSource);
                StopExtensionServer();
                SaveSettingsFromUi();
                UpdateStatus(L("Status.MirroringStopped"));
            }
            finally
            {
                _isStoppingMirroring = false;
            }
        }

        private void RestorePreviousSession()
        {
            if (_settings == null || _settings.PreviousSource == null)
            {
                UpdateStatus(L("Status.NoPreviousSource"));
                return;
            }

            SourceWindowInfo previous = _windowEnumerator.FindByPreviousSource(_settings.PreviousSource);
            if (previous == null)
            {
                UpdateStatus(L("Status.PreviousSourceNotFound"));
                Show();
                Activate();
                RefreshWindowList();
                return;
            }

            RefreshWindowList();
            foreach (SourceWindowInfo item in WindowListView.Items)
            {
                if (item.HandleValue == previous.HandleValue)
                {
                    WindowListView.SelectedItem = item;
                    break;
                }
            }

            StartOverlay();
        }

        private void OpenCropEditor()
        {
            SourceWindowInfo source = GetSelectedSource();
            if (source == null && _sessionManager.CurrentSession != null)
            {
                source = _sessionManager.CurrentSession.Source;
            }

            if (source == null)
            {
                UpdateStatus(L("Status.SelectSourceBeforeCrop"));
                return;
            }

            using (Bitmap preview = _captureService.Capture(source, CaptureMode.FullWindow, CropRegion.Full))
            {
                var cropWindow = new CropWindow(preview, _settings.CropRegion, _localizer);
                cropWindow.Owner = this.IsVisible ? this : null;
                if (cropWindow.ShowDialog() == true)
                {
                    _settings.CropRegion = cropWindow.SelectedRegion;
                    _settings.CaptureMode = CaptureMode.Crop;
                    CropRadioButton.IsChecked = true;
                    _settingsService.Save(_settings);
                    if (_overlayWindow != null)
                    {
                        _overlayWindow.UpdateCrop(_settings.CropRegion, _settings.CaptureMode);
                    }

                    UpdateStatus(L("Status.CropUpdated"));
                }
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow(_settings, _localizer);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                _settings.AutoRestorePreviousSessionOnStartup = settingsWindow.AutoRestorePreviousSessionOnStartup;
                _settings.StartWithWindows = settingsWindow.StartWithWindows;
                _settings.Language = settingsWindow.SelectedLanguage;
                _localizer = new LocalizationService(_settings.Language);
                _settingsService.Save(_settings);
                RegisterHotkeys();
                ApplyLocalization();
                RecreateTrayIcon();
                if (_overlayWindow != null)
                {
                    _overlayWindow.SetLanguage(_settings.Language);
                }

                UpdateStatus(L("Status.SettingsSaved"));
            }
        }

        private void CapturePreviewFrame()
        {
            SourceWindowInfo selected = GetSelectedSource();
            if (selected == null)
            {
                return;
            }

            try
            {
                using (Bitmap bitmap = _captureService.Capture(selected, CaptureMode.FullWindow, CropRegion.Full))
                {
                    PreviewImage.Source = ImageConversion.ToBitmapSource(bitmap);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message);
            }
        }

        private SourceWindowInfo GetSelectedSource()
        {
            return WindowListView.SelectedItem as SourceWindowInfo;
        }

        private async Task<bool> StartExtensionServerAsync()
        {
            // Make sure any in-flight stop has finished before we start.
            await _pendingStopTask.ConfigureAwait(true);

            try
            {
                await _extensionFrameServer.StartAsync().ConfigureAwait(false);
                UpdateStatus(_localizer.Format("Main.ExtensionServerReady", _extensionFrameServer.WebSocketUri));
                return true;
            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message);
                return false;
            }
        }

        private void StopExtensionServer()
        {
            if (!_extensionFrameServer.IsRunning)
            {
                return;
            }

            // Offload to a background thread.  The NotifyIcon (system tray) runs on
            // the WPF UI thread — blocking it with GetAwaiter().GetResult() freezes
            // the tray icon for the entire shutdown duration (up to 2 s).
            _pendingStopTask = Task.Run(async () =>
            {
                try
                {
                    await _extensionFrameServer.StopAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Best-effort shutdown; keep exit resilient.
                }
            });
        }

        private void ExtensionFrameServer_ClientConnected(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                UpdateStatus(L("Status.ExtensionClientConnected"));
            }));
        }

        private void ExtensionFrameServer_ClientDisconnected(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                UpdateStatus(L("Status.ExtensionClientDisconnected"));
            }));
        }

        private void ExtensionFrameServer_FrameReceived(object sender, ExtensionFrameReceivedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (_settings != null &&
                    _settings.CaptureSourceKind == CaptureSourceKind.BrowserExtension &&
                    DateTime.UtcNow - _lastExtensionStatusUtc > TimeSpan.FromSeconds(2))
                {
                    _lastExtensionStatusUtc = DateTime.UtcNow;
                    UpdateStatus(L("Status.ExtensionFrameReceived"));
                }
            }));
        }

        private void ApplySettingsToUi()
        {
            OpacitySlider.Value = _settings.OpacityPercent;
            AspectRatioCheckBox.IsChecked = _settings.AspectRatioLocked;
            FullWindowRadioButton.IsChecked = _settings.CaptureMode == CaptureMode.FullWindow;
            CropRadioButton.IsChecked = _settings.CaptureMode == CaptureMode.Crop;
            SelectComboItem(CaptureSourceComboBox, _settings.CaptureSourceKind.ToString());
            SelectComboItem(FrameRateComboBox, _settings.FrameRateMode.ToString());
            SelectPlacementItem();
            UpdateCaptureSourceUi();
        }

        private void SaveSettingsFromUi()
        {
            if (_settings == null)
            {
                _settings = AppSettings.CreateDefault();
            }

            _settings.OpacityPercent = (int)OpacitySlider.Value;
            _settings.AspectRatioLocked = AspectRatioCheckBox.IsChecked == true;
            _settings.Language = _localizer == null ? _settings.Language : _localizer.Language;
            _settings.CaptureMode = CropRadioButton.IsChecked == true ? CaptureMode.Crop : CaptureMode.FullWindow;
            _settings.CaptureSourceKind = ReadCaptureSourceKind();
            _settings.FrameRateMode = ReadFrameRateMode(FrameRateComboBox);
            _settings.SourcePlacement = ReadPlacementOptions();

            if (_overlayWindow != null)
            {
                _settings.OverlayBounds = new WindowBounds((int)_overlayWindow.Left, (int)_overlayWindow.Top, (int)_overlayWindow.Width, (int)_overlayWindow.Height);
            }

            _settingsService.Save(_settings);
        }

        private void SelectPlacementItem()
        {
            string tag = "KeepOriginalPosition";
            if (_settings.SourcePlacement.Mode == SourcePlacementMode.MoveToScreenEdge)
            {
                tag = "MoveRightEdge";
            }
            else if (_settings.SourcePlacement.Mode == SourcePlacementMode.MoveMostlyOffScreen)
            {
                tag = "MoveMostlyOffScreen";
            }

            SelectComboItem(PlacementComboBox, tag);
        }

        private SourcePlacementOptions ReadPlacementOptions()
        {
            var options = new SourcePlacementOptions();
            string tag = ReadSelectedTag(PlacementComboBox);
            if (tag == "MoveRightEdge")
            {
                options.Mode = SourcePlacementMode.MoveToScreenEdge;
                options.Edge = ScreenEdge.Right;
            }
            else if (tag == "MoveMostlyOffScreen")
            {
                options.Mode = SourcePlacementMode.MoveMostlyOffScreen;
            }

            return options;
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

        private CaptureSourceKind ReadCaptureSourceKind()
        {
            string tag = ReadSelectedTag(CaptureSourceComboBox);
            CaptureSourceKind value;
            if (Enum.TryParse(tag, out value))
            {
                return value;
            }

            return CaptureSourceKind.Window;
        }

        private bool IsExtensionCaptureSelected()
        {
            return ReadCaptureSourceKind() == CaptureSourceKind.BrowserExtension;
        }

        private void UpdateCaptureSourceUi()
        {
            bool extension = IsExtensionCaptureSelected();
            WindowListView.IsEnabled = !extension;
            WindowSearchTextBox.IsEnabled = !extension;
            ShowAllWindowsCheckBox.IsEnabled = !extension;
            RefreshButton.IsEnabled = !extension;
            RestorePreviousButton.IsEnabled = !extension;
            RestoreSourceButton.IsEnabled = !extension;
            PlacementComboBox.IsEnabled = !extension;

            ExtensionStatusTextBlock.Text = extension
                ? (_extensionFrameServer.IsRunning ? _localizer.Format("Main.ExtensionServerReady", _extensionFrameServer.WebSocketUri) : L("Main.ExtensionWaiting"))
                : string.Empty;
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
            if (_overlayWindow == null)
            {
                return;
            }

            if (_overlayWindow.IsVisible)
            {
                _overlayWindow.Hide();
            }
            else
            {
                _overlayWindow.Show();
            }
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
        }

        private void HandlePendingRecovery()
        {
            if (!_recoveryService.HasPendingSession)
            {
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                L("Recovery.Prompt"),
                L("Recovery.Title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                WindowPlacementSnapshot snapshot = _recoveryService.Load();
                _sourceWindowManager.Restore(snapshot);
                _recoveryService.Clear();
                UpdateStatus(L("Status.RecoveredSource"));
            }
        }

        private void CreateTrayIcon()
        {
            if (_trayIcon != null)
            {
                return;
            }

            _trayIcon = new Forms.NotifyIcon();
            _trayIcon.Text = "PlayPane";
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate { QueueUiAction(ShowControlPanel); };
            _trayIcon.MouseUp += TrayIcon_MouseUp;

            _trayMenu = new Forms.ContextMenuStrip();
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.ShowControlPanel"), ShowControlPanel));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.ShowOverlay"), delegate { if (_overlayWindow != null) _overlayWindow.Show(); }));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.HideOverlay"), delegate { if (_overlayWindow != null) _overlayWindow.Hide(); }));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.EnterEditMode"), delegate { if (_overlayWindow != null) _overlayWindow.EnterEditMode(); }));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.EnterGameMode"), delegate { if (_overlayWindow != null) _overlayWindow.EnterGameMode(); }));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.StopMirroring"), delegate { StopMirroring(true); }));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.RestoreSource"), delegate { _sessionManager.RestoreSource(); }));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.Settings"), OpenSettings));
            _trayMenu.Items.Add(CreateTrayMenuItem(L("Tray.Exit"), ExitApplication));
        }

        private Forms.ToolStripMenuItem CreateTrayMenuItem(string text, Action action)
        {
            var item = new Forms.ToolStripMenuItem(text);
            item.Click += delegate { QueueUiAction(action); };
            return item;
        }

        private void TrayIcon_MouseUp(object sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.Right && _trayMenu != null)
            {
                _trayMenu.Show(Forms.Control.MousePosition);
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
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.MouseUp -= TrayIcon_MouseUp;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_trayMenu != null)
            {
                _trayMenu.Dispose();
                _trayMenu = null;
            }

            CreateTrayIcon();
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

        private void OverlayWindow_CropRequested(object sender, EventArgs e)
        {
            ShowControlPanel();
            OpenCropEditor();
        }

        private void OverlayWindow_HiddenRequested(object sender, EventArgs e)
        {
            UpdateStatus(L("Status.OverlayHidden"));
        }

        private void OverlayWindow_Closed(object sender, EventArgs e)
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.StopCapture();
                _overlayWindow = null;
            }
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = message;
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
            RestorePreviousButton.Content = L("Main.RestorePrevious");
            SettingsButton.Content = L("Main.Settings");
            SubtitleTextBlock.Text = L("Main.Subtitle");
            RefreshButton.Content = L("Main.Refresh");
            ShowAllWindowsCheckBox.Content = L("Main.ShowAllWindows");
            BrowserColumn.Header = L("Main.ColumnBrowser");
            TitleColumn.Header = L("Main.ColumnTitle");
            ProcessColumn.Header = L("Main.ColumnProcess");
            MonitorColumn.Header = L("Main.ColumnMonitor");
            SourcePreviewLabel.Text = L("Main.SourcePreview");
            CaptureSourceLabel.Text = L("Main.CaptureSource");
            WindowCaptureSourceItem.Content = L("Main.WindowCaptureSource");
            ExtensionCaptureSourceItem.Content = L("Main.ExtensionCaptureSource");
            MirroringLabel.Text = L("Main.Mirroring");
            FullWindowRadioButton.Content = L("Main.FullWindow");
            CropRadioButton.Content = L("Main.CropRegion");
            EditCropButton.Content = L("Main.EditCrop");
            FrameRateLabel.Text = L("Main.FrameRate");
            FrameRateLowItem.Content = L("Main.FrameRateLow");
            FrameRateStandardItem.Content = L("Main.FrameRateStandard");
            FrameRateSmoothItem.Content = L("Main.FrameRateSmooth");
            OpacityLabel.Text = L("Main.Opacity");
            AspectRatioCheckBox.Content = L("Main.LockAspectRatio");
            SourcePlacementLabel.Text = L("Main.SourcePlacement");
            KeepOriginalPositionItem.Content = L("Main.KeepOriginalPosition");
            MoveRightEdgeItem.Content = L("Main.MoveRightEdge");
            MoveMostlyOffScreenItem.Content = L("Main.MoveMostlyOffScreen");
            StartOverlayButton.Content = L("Main.StartOverlay");
            StopMirroringButton.Content = L("Main.StopMirroring");
            RestoreSourceButton.Content = L("Main.RestoreSource");

            StatusTextBlock.Text = L("App.Ready");
        }

        private static bool Contains(string value, string query)
        {
            return value != null && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
