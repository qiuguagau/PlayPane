using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PlayPane.Core.Models;
using PlayPane.Core.Services;
using DrawingBitmap = System.Drawing.Bitmap;

namespace PlayPane
{
    public partial class CropWindow : Window
    {
        private readonly int _sourceWidth;
        private readonly int _sourceHeight;
        private System.Windows.Point _startPoint;
        private Rect _startRect;
        private CropDragMode _dragMode;
        private bool _isDragging;
        private readonly LocalizationService _localizer;

        public CropWindow(DrawingBitmap preview, CropRegion initialRegion, LocalizationService localizer)
        {
            InitializeComponent();
            _localizer = localizer;
            ApplyLocalization();
            _sourceWidth = preview.Width;
            _sourceHeight = preview.Height;
            SelectedRegion = initialRegion == null ? CropRegion.Full : initialRegion.Clamp();
            PreviewImage.Source = ImageConversion.ToBitmapSource(preview);
            Loaded += delegate { DrawCropRectangle(); };
            SizeChanged += delegate { DrawCropRectangle(); };
        }

        public CropRegion SelectedRegion { get; private set; }

        private void ApplyLocalization()
        {
            Title = _localizer.Get("Crop.Title");
            ResetButton.Content = _localizer.Get("Common.Reset");
            ConfirmButton.Content = _localizer.Get("Common.Confirm");
            CancelButton.Content = _localizer.Get("Common.Cancel");
        }

        private void PreviewHost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(PreviewHost);
            _startRect = GetVisualCropRect();
            _dragMode = HitTestCrop(_startPoint, _startRect);
            _isDragging = true;
            PreviewHost.CaptureMouse();
        }

        private void PreviewHost_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging)
            {
                return;
            }

            System.Windows.Point current = e.GetPosition(PreviewHost);
            Rect imageRect = GetImageRect();
            Rect selection = ResolveDragRect(current, imageRect);
            DrawVisualRect(selection);
            SelectedRegion = RectToRegion(selection, imageRect);
            UpdateCropInfo();
        }

        private void PreviewHost_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _dragMode = CropDragMode.None;
            PreviewHost.ReleaseMouseCapture();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedRegion = CropRegion.Full;
            DrawCropRectangle();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void DrawCropRectangle()
        {
            DrawVisualRect(GetVisualCropRect());
            UpdateCropInfo();
        }

        private Rect GetVisualCropRect()
        {
            Rect imageRect = GetImageRect();
            PixelRect pixels = SelectedRegion.ToPixels(_sourceWidth, _sourceHeight);
            double x = imageRect.X + (pixels.X / (double)_sourceWidth) * imageRect.Width;
            double y = imageRect.Y + (pixels.Y / (double)_sourceHeight) * imageRect.Height;
            double width = (pixels.Width / (double)_sourceWidth) * imageRect.Width;
            double height = (pixels.Height / (double)_sourceHeight) * imageRect.Height;
            return new Rect(x, y, width, height);
        }

        private void DrawVisualRect(Rect rect)
        {
            Canvas.SetLeft(CropRectangle, rect.X);
            Canvas.SetTop(CropRectangle, rect.Y);
            CropRectangle.Width = Math.Max(1, rect.Width);
            CropRectangle.Height = Math.Max(1, rect.Height);
        }

        private void UpdateCropInfo()
        {
            PixelRect pixels = SelectedRegion.ToPixels(_sourceWidth, _sourceHeight);
            CropInfoTextBlock.Text = pixels.Width + " x " + pixels.Height + " | Aspect " + SelectedRegion.GetAspectRatio(_sourceWidth, _sourceHeight).ToString("0.###");
        }

        private Rect GetImageRect()
        {
            double hostWidth = PreviewHost.ActualWidth;
            double hostHeight = PreviewHost.ActualHeight;
            if (hostWidth <= 0 || hostHeight <= 0)
            {
                return new Rect(0, 0, 1, 1);
            }

            double imageRatio = _sourceWidth / (double)_sourceHeight;
            double hostRatio = hostWidth / hostHeight;
            double width;
            double height;

            if (hostRatio > imageRatio)
            {
                height = hostHeight;
                width = height * imageRatio;
            }
            else
            {
                width = hostWidth;
                height = width / imageRatio;
            }

            return new Rect((hostWidth - width) / 2, (hostHeight - height) / 2, width, height);
        }

        private CropRegion RectToRegion(Rect selection, Rect imageRect)
        {
            if (selection.Width < 2 || selection.Height < 2)
            {
                return SelectedRegion;
            }

            double x = (selection.X - imageRect.X) / imageRect.Width;
            double y = (selection.Y - imageRect.Y) / imageRect.Height;
            double width = selection.Width / imageRect.Width;
            double height = selection.Height / imageRect.Height;
            return new CropRegion(x, y, width, height).Clamp();
        }

        private Rect ResolveDragRect(System.Windows.Point current, Rect imageRect)
        {
            Rect selection;
            double dx = current.X - _startPoint.X;
            double dy = current.Y - _startPoint.Y;

            if (_dragMode == CropDragMode.New)
            {
                selection = NormalizeRect(new Rect(_startPoint, current));
            }
            else if (_dragMode == CropDragMode.Move)
            {
                selection = _startRect;
                selection.Offset(dx, dy);
            }
            else
            {
                double left = _startRect.Left;
                double top = _startRect.Top;
                double right = _startRect.Right;
                double bottom = _startRect.Bottom;

                if (_dragMode == CropDragMode.Left || _dragMode == CropDragMode.TopLeft || _dragMode == CropDragMode.BottomLeft)
                {
                    left = current.X;
                }

                if (_dragMode == CropDragMode.Right || _dragMode == CropDragMode.TopRight || _dragMode == CropDragMode.BottomRight)
                {
                    right = current.X;
                }

                if (_dragMode == CropDragMode.Top || _dragMode == CropDragMode.TopLeft || _dragMode == CropDragMode.TopRight)
                {
                    top = current.Y;
                }

                if (_dragMode == CropDragMode.Bottom || _dragMode == CropDragMode.BottomLeft || _dragMode == CropDragMode.BottomRight)
                {
                    bottom = current.Y;
                }

                selection = NormalizeRect(new Rect(new System.Windows.Point(left, top), new System.Windows.Point(right, bottom)));
            }

            return ClampRect(selection, imageRect, _dragMode == CropDragMode.Move);
        }

        private static Rect ClampRect(Rect rect, Rect bounds, bool preserveSize)
        {
            double minSize = 10;
            rect.Intersect(new Rect(bounds.X - bounds.Width, bounds.Y - bounds.Height, bounds.Width * 3, bounds.Height * 3));

            if (preserveSize)
            {
                double x = rect.X;
                double y = rect.Y;
                if (x < bounds.X)
                {
                    x = bounds.X;
                }

                if (y < bounds.Y)
                {
                    y = bounds.Y;
                }

                if (x + rect.Width > bounds.Right)
                {
                    x = bounds.Right - rect.Width;
                }

                if (y + rect.Height > bounds.Bottom)
                {
                    y = bounds.Bottom - rect.Height;
                }

                return new Rect(x, y, rect.Width, rect.Height);
            }

            double left = Math.Max(bounds.Left, rect.Left);
            double top = Math.Max(bounds.Top, rect.Top);
            double right = Math.Min(bounds.Right, rect.Right);
            double bottom = Math.Min(bounds.Bottom, rect.Bottom);

            if (right - left < minSize)
            {
                right = Math.Min(bounds.Right, left + minSize);
            }

            if (bottom - top < minSize)
            {
                bottom = Math.Min(bounds.Bottom, top + minSize);
            }

            return new Rect(left, top, Math.Max(minSize, right - left), Math.Max(minSize, bottom - top));
        }

        private static CropDragMode HitTestCrop(System.Windows.Point point, Rect rect)
        {
            const double edge = 10;
            bool nearLeft = Math.Abs(point.X - rect.Left) <= edge;
            bool nearRight = Math.Abs(point.X - rect.Right) <= edge;
            bool nearTop = Math.Abs(point.Y - rect.Top) <= edge;
            bool nearBottom = Math.Abs(point.Y - rect.Bottom) <= edge;

            if (nearLeft && nearTop)
            {
                return CropDragMode.TopLeft;
            }

            if (nearRight && nearTop)
            {
                return CropDragMode.TopRight;
            }

            if (nearLeft && nearBottom)
            {
                return CropDragMode.BottomLeft;
            }

            if (nearRight && nearBottom)
            {
                return CropDragMode.BottomRight;
            }

            if (nearLeft)
            {
                return CropDragMode.Left;
            }

            if (nearRight)
            {
                return CropDragMode.Right;
            }

            if (nearTop)
            {
                return CropDragMode.Top;
            }

            if (nearBottom)
            {
                return CropDragMode.Bottom;
            }

            if (rect.Contains(point))
            {
                return CropDragMode.Move;
            }

            return CropDragMode.New;
        }

        private static Rect NormalizeRect(Rect rect)
        {
            double x = Math.Min(rect.Left, rect.Right);
            double y = Math.Min(rect.Top, rect.Bottom);
            double width = Math.Abs(rect.Width);
            double height = Math.Abs(rect.Height);
            return new Rect(x, y, width, height);
        }

        private enum CropDragMode
        {
            None,
            New,
            Move,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }
    }
}
