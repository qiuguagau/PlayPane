using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using PlayPane.Core.Capture;
using PlayPane.Core.Input;
using PlayPane.Core.Models;
using PlayPane.Core.Native;
using PlayPane.Core.Services;

namespace PlayPane
{
    public partial class OverlayWindow : Window
    {
        private readonly IWindowCaptureService _captureService;
        private readonly FrameRateController _frameRateController;
        private readonly SettingsService _settingsService;
        private readonly ClickThroughService _clickThroughService = new ClickThroughService();
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private CaptureSession _session;
        private AppSettings _settings;
        private DateTime _lastValidFrameUtc;
        private LocalizationService _localizer = new LocalizationService(AppLanguage.English);

        public OverlayWindow(IWindowCaptureService captureService, FrameRateController frameRateController, SettingsService settingsService)
        {
            InitializeComponent();
            _captureService = captureService;
            _frameRateController = frameRateController;
            _settingsService = settingsService;
            _timer.Tick += Timer_Tick;
            Activated += OverlayWindow_Activated;
            Closing += OverlayWindow_Closing;
        }

        public event EventHandler StopRequested;

        public event EventHandler CropRequested;

        public event EventHandler HiddenRequested;

        public bool IsGameMode { get; private set; }

        public void Configure(CaptureSession session, AppSettings settings)
        {
            _session = session;
            _settings = settings;
            SetLanguage(settings.Language);
            Left = settings.OverlayBounds.X;
            Top = settings.OverlayBounds.Y;
            Width = settings.OverlayBounds.Width;
            Height = settings.OverlayBounds.Height;
            SetOpacityPercent(settings.OpacityPercent);
            MirrorImage.Stretch = settings.AspectRatioLocked ? Stretch.Uniform : Stretch.Fill;
            SelectFrameRate(settings.FrameRateMode);
            UpdateTimerInterval();
            _timer.Start();
        }

        public void StopCapture()
        {
            _timer.Stop();
        }

        public void SetLanguage(AppLanguage language)
        {
            _localizer = new LocalizationService(language);
            Title = _localizer.Get("Overlay.Title");
            ToolbarTitle.Text = _localizer.Get("Overlay.Title");
            OpacityLabel.Text = _localizer.Get("Overlay.Opacity");
            CropButton.Content = _localizer.Get("Overlay.Crop");
            LockButton.Content = _localizer.Get("Overlay.Lock");
            HideButton.Content = _localizer.Get("Overlay.Hide");
            StopButton.Content = _localizer.Get("Overlay.Stop");
        }

        public void EnterEditMode()
        {
            IsGameMode = false;
            Toolbar.Visibility = Visibility.Visible;
            OuterBorder.BorderThickness = new Thickness(2);
            ResizeMode = ResizeMode.CanResizeWithGrip;
            _clickThroughService.SetClickThrough(new WindowInteropHelper(this).Handle, false);
            ShowActivated = true;
        }

        public void EnterGameMode()
        {
            IsGameMode = true;
            Toolbar.Visibility = Visibility.Collapsed;
            OuterBorder.BorderThickness = new Thickness(0);
            ResizeMode = ResizeMode.NoResize;
            _clickThroughService.SetClickThrough(new WindowInteropHelper(this).Handle, true);
            Topmost = true;
        }

        public void SetOpacityPercent(int opacityPercent)
        {
            int value = Math.Max(10, Math.Min(100, opacityPercent));
            MirrorImage.Opacity = value / 100.0;
            OpacitySlider.Value = value;
            if (_settings != null)
            {
                _settings.OpacityPercent = value;
                _settingsService.Save(_settings);
            }
        }

        public void UpdateCrop(CropRegion cropRegion, PlayPane.Core.Models.CaptureMode mode)
        {
            if (_settings != null)
            {
                _settings.CropRegion = cropRegion;
                _settings.CaptureMode = mode;
                _settingsService.Save(_settings);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_session == null || _settings == null || !IsVisible)
            {
                return;
            }

            try
            {
                if (Win32Api.IsIconic(_session.Source.Handle))
                {
                    ShowWarning(_localizer.Get("Overlay.SourceMinimized"));
                }

                using (Bitmap bitmap = _captureService.Capture(_session.Source, _settings.CaptureMode, _settings.CropRegion))
                {
                    MirrorImage.Source = ImageConversion.ToBitmapSource(bitmap);
                    _lastValidFrameUtc = DateTime.UtcNow;
                    if (!Win32Api.IsIconic(_session.Source.Handle))
                    {
                        WarningBorder.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowWarning(ex.Message);
            }

            if (_lastValidFrameUtc != DateTime.MinValue && DateTime.UtcNow - _lastValidFrameUtc > TimeSpan.FromSeconds(5))
            {
                ShowWarning(_localizer.Get("Overlay.FramePaused"));
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
            if (e.ClickCount == 2)
            {
                EnterGameMode();
                return;
            }

            DragMove();
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
            UpdateTimerInterval();
        }

        private void CropButton_Click(object sender, RoutedEventArgs e)
        {
            EventHandler handler = CropRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
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

        private void UpdateTimerInterval()
        {
            FrameRateMode mode = _settings == null ? FrameRateMode.Standard : _settings.FrameRateMode;
            _timer.Interval = _frameRateController.GetInterval(mode);
        }

        private void ShowWarning(string message)
        {
            WarningTextBlock.Text = message;
            WarningBorder.Visibility = Visibility.Visible;
        }
    }
}
