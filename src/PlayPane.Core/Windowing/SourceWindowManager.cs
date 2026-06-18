using System;
using PlayPane.Core.Models;
using PlayPane.Core.Native;

namespace PlayPane.Core.Windowing
{
    public sealed class SourceWindowManager
    {
        private readonly DisplayManager _displayManager;

        public SourceWindowManager()
            : this(new DisplayManager())
        {
        }

        public SourceWindowManager(DisplayManager displayManager)
        {
            _displayManager = displayManager;
        }

        public WindowPlacementSnapshot CaptureSnapshot(SourceWindowInfo source)
        {
            if (source == null)
            {
                return null;
            }

            var placement = new Win32Api.WINDOWPLACEMENT();
            placement.Length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win32Api.WINDOWPLACEMENT));
            Win32Api.GetWindowPlacement(source.Handle, ref placement);

            return new WindowPlacementSnapshot
            {
                HandleValue = source.HandleValue,
                Title = source.Title,
                ProcessName = source.ProcessName,
                BrowserType = source.BrowserType,
                Bounds = source.Bounds,
                MonitorDeviceName = source.MonitorDeviceName,
                IsMaximized = placement.ShowCmd == Win32Api.SW_SHOWMAXIMIZED,
                ShowCommand = placement.ShowCmd
            };
        }

        public void ApplyPlacement(SourceWindowInfo source, SourcePlacementOptions options)
        {
            if (source == null || options == null || options.Mode == SourcePlacementMode.KeepOriginalPosition)
            {
                return;
            }

            if (source.IsMinimized)
            {
                Win32Api.ShowWindow(source.Handle, Win32Api.SW_RESTORE);
            }

            if (options.Mode == SourcePlacementMode.MoveToScreenEdge)
            {
                MoveToScreenEdge(source, options.Edge, options.VisiblePixels);
                return;
            }

            if (options.Mode == SourcePlacementMode.MoveToAnotherMonitor)
            {
                MoveToMonitor(source, options.TargetMonitorDeviceName, options.MonitorAnchor);
                return;
            }

            if (options.Mode == SourcePlacementMode.MoveMostlyOffScreen)
            {
                MoveMostlyOffScreen(source, options.VisiblePixels);
            }
        }

        public void Restore(WindowPlacementSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            WindowBounds bounds = snapshot.Bounds ?? new WindowBounds(40, 40, 800, 600);
            WindowBounds visible = _displayManager.EnsureVisible(bounds);

            Win32Api.ShowWindow(snapshot.Handle, Win32Api.SW_RESTORE);
            Win32Api.MoveWindow(snapshot.Handle, visible.X, visible.Y, visible.Width, visible.Height, true);

            if (snapshot.IsMaximized)
            {
                Win32Api.ShowWindow(snapshot.Handle, Win32Api.SW_SHOWMAXIMIZED);
            }
        }

        private void MoveToScreenEdge(SourceWindowInfo source, ScreenEdge edge, int visiblePixels)
        {
            WindowBounds bounds = source.Bounds;
            WindowBounds work = _displayManager.FromBounds(bounds).WorkingArea;
            int visible = NormalizeVisiblePixels(visiblePixels);
            int x = bounds.X;
            int y = bounds.Y;

            if (edge == ScreenEdge.Left)
            {
                x = work.X - bounds.Width + visible;
                y = work.Y;
            }
            else if (edge == ScreenEdge.Right)
            {
                x = work.X + work.Width - visible;
                y = work.Y;
            }
            else if (edge == ScreenEdge.Top)
            {
                x = work.X;
                y = work.Y - bounds.Height + visible;
            }
            else if (edge == ScreenEdge.Bottom)
            {
                x = work.X;
                y = work.Y + work.Height - visible;
            }

            Win32Api.MoveWindow(source.Handle, x, y, bounds.Width, bounds.Height, true);
        }

        private void MoveMostlyOffScreen(SourceWindowInfo source, int visiblePixels)
        {
            WindowBounds bounds = source.Bounds;
            WindowBounds work = _displayManager.FromBounds(bounds).WorkingArea;
            int visible = NormalizeVisiblePixels(visiblePixels);
            int x = work.X + work.Width - visible;
            int y = work.Y + visible;

            Win32Api.MoveWindow(source.Handle, x, y, bounds.Width, bounds.Height, true);
        }

        private void MoveToMonitor(SourceWindowInfo source, string monitorDeviceName, MonitorAnchor anchor)
        {
            WindowBounds bounds = source.Bounds;
            WindowBounds work = _displayManager.FindByDeviceName(monitorDeviceName).WorkingArea;

            if (anchor == MonitorAnchor.Maximize)
            {
                Win32Api.MoveWindow(source.Handle, work.X, work.Y, work.Width, work.Height, true);
                Win32Api.ShowWindow(source.Handle, Win32Api.SW_SHOWMAXIMIZED);
                return;
            }

            int width = Math.Min(bounds.Width, work.Width);
            int height = Math.Min(bounds.Height, work.Height);
            int x = work.X;
            int y = work.Y;

            if (anchor == MonitorAnchor.TopRight || anchor == MonitorAnchor.BottomRight)
            {
                x = work.X + work.Width - width;
            }
            else if (anchor == MonitorAnchor.Center)
            {
                x = work.X + (work.Width - width) / 2;
            }

            if (anchor == MonitorAnchor.BottomLeft || anchor == MonitorAnchor.BottomRight)
            {
                y = work.Y + work.Height - height;
            }
            else if (anchor == MonitorAnchor.Center)
            {
                y = work.Y + (work.Height - height) / 2;
            }

            Win32Api.MoveWindow(source.Handle, x, y, width, height, true);
        }

        private static int NormalizeVisiblePixels(int visiblePixels)
        {
            if (visiblePixels < 20)
            {
                return 20;
            }

            if (visiblePixels > 40)
            {
                return 40;
            }

            return visiblePixels;
        }
    }
}
