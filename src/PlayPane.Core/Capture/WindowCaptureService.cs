using System;
using System.Drawing;
using System.Drawing.Imaging;
using PlayPane.Core.Models;
using PlayPane.Core.Native;
using PlayPane.Core.Services;

namespace PlayPane.Core.Capture
{
    public sealed class WindowCaptureService : IWindowCaptureService
    {
        private readonly CropProcessor _cropProcessor;

        public WindowCaptureService()
            : this(new CropProcessor())
        {
        }

        public WindowCaptureService(CropProcessor cropProcessor)
        {
            _cropProcessor = cropProcessor;
        }

        public Bitmap Capture(SourceWindowInfo source, CaptureMode mode, CropRegion cropRegion)
        {
            if (source == null)
            {
                throw new WindowCaptureException("No source window is selected.");
            }

            WindowBounds bounds = GetCurrentBounds(source);
            if (!bounds.IsUsable)
            {
                throw new WindowCaptureException("The source window is too small to capture.");
            }

            Bitmap fullFrame = CaptureFullWindow(source.Handle, bounds);
            if (mode != CaptureMode.Crop)
            {
                return fullFrame;
            }

            try
            {
                PixelRect crop = _cropProcessor.Resolve(cropRegion, fullFrame.Width, fullFrame.Height);
                Rectangle cropRectangle = new Rectangle(crop.X, crop.Y, crop.Width, crop.Height);
                return fullFrame.Clone(cropRectangle, PixelFormat.Format32bppArgb);
            }
            finally
            {
                fullFrame.Dispose();
            }
        }

        private static WindowBounds GetCurrentBounds(SourceWindowInfo source)
        {
            Win32Api.RECT rect;
            if (!Win32Api.GetWindowRect(source.Handle, out rect))
            {
                return source.Bounds;
            }

            return new WindowBounds(rect.Left, rect.Top, Math.Max(0, rect.Right - rect.Left), Math.Max(0, rect.Bottom - rect.Top));
        }

        private static Bitmap CaptureFullWindow(IntPtr handle, WindowBounds bounds)
        {
            var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    bool printed = Win32Api.PrintWindow(handle, hdc, 2);
                    if (!printed)
                    {
                        graphics.ReleaseHdc(hdc);
                        hdc = IntPtr.Zero;
                        using (Graphics fallback = Graphics.FromImage(bitmap))
                        {
                            fallback.CopyFromScreen(bounds.X, bounds.Y, 0, 0, new Size(bounds.Width, bounds.Height), CopyPixelOperation.SourceCopy);
                        }
                    }
                }
                finally
                {
                    if (hdc != IntPtr.Zero)
                    {
                        graphics.ReleaseHdc(hdc);
                    }
                }
            }

            return bitmap;
        }
    }
}
