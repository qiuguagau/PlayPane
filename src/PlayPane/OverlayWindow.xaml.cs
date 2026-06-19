using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PlayPane.Core.Input;
using PlayPane.Core.Models;
using PlayPane.Core.Native;
using PlayPane.Core.Services;

namespace PlayPane
{
    public partial class OverlayWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly FrameRateController _frameRateController = new FrameRateController();
        private readonly ClickThroughService _clickThroughService = new ClickThroughService();
        private AppSettings _settings;
        private LocalizationService _localizer = new LocalizationService(AppLanguage.English);
        private Uri _webRtcSignalingUri;
        private bool _isRestartingViewer;

        public OverlayWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            Activated += OverlayWindow_Activated;
            Closing += OverlayWindow_Closing;
        }

        public event EventHandler StopRequested;

        public event EventHandler HiddenRequested;

        public bool IsGameMode { get; private set; }

        public void Configure(CaptureSession session, AppSettings settings, Uri webRtcSignalingUri)
        {
            _settings = settings;
            _webRtcSignalingUri = webRtcSignalingUri;
            SetLanguage(settings.Language);
            Left = settings.OverlayBounds.X;
            Top = settings.OverlayBounds.Y;
            Width = settings.OverlayBounds.Width;
            Height = settings.OverlayBounds.Height;
            SetOpacityPercent(settings.OpacityPercent);
            SelectFrameRate(settings.FrameRateMode);
            InitializeWebRtcViewer();
        }

        public void StopCapture()
        {
            _ = StopWebRtcViewerAsync();
        }

        public void SetLanguage(AppLanguage language)
        {
            _localizer = new LocalizationService(language);
            Title = _localizer.Get("Overlay.Title");
            ToolbarTitle.Text = _localizer.Get("Overlay.Title");
            OpacityLabel.Text = _localizer.Get("Overlay.Opacity");
            LockButton.Content = _localizer.Get("Overlay.Lock");
            HideButton.Content = _localizer.Get("Overlay.Hide");
            StopButton.Content = _localizer.Get("Overlay.Stop");
        }

        public void EnterEditMode()
        {
            OverlayModePolicy policy = OverlayModePolicy.ForEditMode();
            IsGameMode = false;
            Topmost = policy.IsTopmost;
            Toolbar.Visibility = Visibility.Visible;
            OuterBorder.BorderThickness = new Thickness(2);
            ResizeMode = ResizeMode.CanResizeWithGrip;
            _clickThroughService.SetClickThrough(new WindowInteropHelper(this).Handle, policy.IsClickThrough);
            ShowActivated = true;
        }

        public void EnterGameMode()
        {
            OverlayModePolicy policy = OverlayModePolicy.ForGameMode();
            IsGameMode = true;
            Topmost = policy.IsTopmost;
            Toolbar.Visibility = Visibility.Collapsed;
            OuterBorder.BorderThickness = new Thickness(0);
            ResizeMode = ResizeMode.NoResize;
            _clickThroughService.SetClickThrough(new WindowInteropHelper(this).Handle, policy.IsClickThrough);
        }

        public void SetOpacityPercent(int opacityPercent)
        {
            int value = Math.Max(10, Math.Min(100, opacityPercent));
            ApplyWebRtcOpacity(value);
            OpacitySlider.Value = value;
            if (_settings != null)
            {
                _settings.OpacityPercent = value;
                _settingsService.Save(_settings);
            }
        }

        private void OverlayWindow_Activated(object sender, EventArgs e)
        {
            if (IsGameMode)
            {
                EnterEditMode();
            }
        }

        private void OverlayWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_settings != null)
            {
                _settings.OverlayBounds = new WindowBounds((int)Left, (int)Top, (int)Width, (int)Height);
                _settingsService.Save(_settings);
            }
        }

        private void Toolbar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsInteractiveToolbarElement(e.OriginalSource as DependencyObject))
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                EnterGameMode();
                return;
            }

            DragMove();
        }

        private bool IsInteractiveToolbarElement(DependencyObject source)
        {
            while (source != null && source != Toolbar)
            {
                if (source is ButtonBase ||
                    source is Slider ||
                    source is ComboBox ||
                    source is Thumb)
                {
                    return true;
                }

                source = GetElementParent(source);
            }

            return false;
        }

        private static DependencyObject GetElementParent(DependencyObject source)
        {
            FrameworkElement element = source as FrameworkElement;
            if (element != null && element.Parent != null)
            {
                return element.Parent;
            }

            FrameworkContentElement contentElement = source as FrameworkContentElement;
            if (contentElement != null)
            {
                return contentElement.Parent;
            }

            try
            {
                return VisualTreeHelper.GetParent(source);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
            {
                return;
            }

            SetOpacityPercent((int)e.NewValue);
        }

        private void FrameRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _settings == null)
            {
                return;
            }

            _settings.FrameRateMode = ReadFrameRateMode();
            _settingsService.Save(_settings);
            RestartWebRtcViewer();
        }

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            EnterGameMode();
        }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            EventHandler handler = HiddenRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            EventHandler handler = StopRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void SelectFrameRate(FrameRateMode mode)
        {
            foreach (object itemObject in FrameRateComboBox.Items)
            {
                ComboBoxItem item = itemObject as ComboBoxItem;
                if (item != null && item.Tag != null && item.Tag.ToString() == mode.ToString())
                {
                    FrameRateComboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private FrameRateMode ReadFrameRateMode()
        {
            ComboBoxItem item = FrameRateComboBox.SelectedItem as ComboBoxItem;
            if (item == null || item.Tag == null)
            {
                return FrameRateMode.Standard;
            }

            FrameRateMode value;
            if (Enum.TryParse(item.Tag.ToString(), out value))
            {
                return value;
            }

            return FrameRateMode.Standard;
        }

        private async void InitializeWebRtcViewer()
        {
            if (_webRtcSignalingUri == null)
            {
                ShowWarning(_localizer.Get("Overlay.WebRtcMissingSignaling"));
                return;
            }

            try
            {
                WebRtcView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                await WebRtcView.EnsureCoreWebView2Async(await WebView2Runtime.GetEnvironmentAsync().ConfigureAwait(true)).ConfigureAwait(true);
                WebRtcView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                WebRtcView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                WebRtcView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                WebRtcView.CoreWebView2.NavigateToString(WebRtcViewerPage.Build(
                    _webRtcSignalingUri,
                    _localizer.Get("Overlay.WebRtcWaiting"),
                    _settings == null || _settings.AspectRatioLocked,
                    _settings == null ? 100 : _settings.OpacityPercent,
                    _frameRateController.GetFramesPerSecond(_settings == null ? FrameRateMode.Standard : _settings.FrameRateMode)));
                ApplyWebRtcOpacity(_settings == null ? 100 : _settings.OpacityPercent);
            }
            catch (Exception ex)
            {
                ShowWarning(ex.Message);
            }
        }

        private async void RestartWebRtcViewer()
        {
            if (_isRestartingViewer)
            {
                return;
            }

            _isRestartingViewer = true;
            try
            {
                await StopWebRtcViewerAsync().ConfigureAwait(true);
                InitializeWebRtcViewer();
            }
            finally
            {
                _isRestartingViewer = false;
            }
        }

        private async System.Threading.Tasks.Task StopWebRtcViewerAsync()
        {
            try
            {
                if (WebRtcView.CoreWebView2 != null)
                {
                    await WebRtcView.CoreWebView2.ExecuteScriptAsync("window.playPaneStopViewer && window.playPaneStopViewer();").ConfigureAwait(true);
                    await System.Threading.Tasks.Task.Delay(80).ConfigureAwait(true);
                    WebRtcView.CoreWebView2.NavigateToString(WebRtcViewerPage.Blank);
                }
            }
            catch
            {
                // The WebView may already be tearing down with the overlay.
            }
        }

        private void ApplyWebRtcOpacity(int opacityPercent)
        {
            if (WebRtcView.CoreWebView2 == null)
            {
                return;
            }

            double opacity = Math.Max(10, Math.Min(100, opacityPercent)) / 100.0;
            string script = "document.documentElement.style.setProperty('--playpane-opacity', '" +
                opacity.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                "');";
            WebRtcView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private void ShowWarning(string message)
        {
            WarningTextBlock.Text = message;
            WarningBorder.Visibility = Visibility.Visible;
        }
    }
}
