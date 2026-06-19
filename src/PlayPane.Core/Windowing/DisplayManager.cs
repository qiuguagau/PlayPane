using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PlayPane.Core.Models;

namespace PlayPane.Core.Windowing
{
    public sealed class DisplayManager
    {
        public IList<MonitorInfo> GetMonitors()
        {
            var monitors = new List<MonitorInfo>();
            foreach (Screen screen in Screen.AllScreens)
            {
                monitors.Add(ToMonitorInfo(screen));
            }

            return monitors;
        }

        public MonitorInfo GetPrimaryMonitor()
        {
            return ToMonitorInfo(Screen.PrimaryScreen);
        }

        public MonitorInfo FindByDeviceName(string deviceName)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.DeviceName == deviceName)
                {
                    return ToMonitorInfo(screen);
                }
            }

            return GetPrimaryMonitor();
        }

        public MonitorInfo FromBounds(WindowBounds bounds)
        {
            Rectangle rectangle = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            return ToMonitorInfo(Screen.FromRectangle(rectangle));
        }

        public WindowBounds EnsureVisible(WindowBounds bounds)
        {
            MonitorInfo monitor = GetPrimaryMonitor();
            WindowBounds work = monitor.WorkingArea;
            int width = bounds.Width > 0 ? bounds.Width : 800;
            int height = bounds.Height > 0 ? bounds.Height : 450;

            if (width > work.Width)
            {
                width = work.Width;
            }

            if (height > work.Height)
            {
                height = work.Height;
            }

            int x = bounds.X;
            int y = bounds.Y;

            if (x < work.X)
            {
                x = work.X;
            }

            if (y < work.Y)
            {
                y = work.Y;
            }

            if (x + width > work.X + work.Width)
            {
                x = work.X + work.Width - width;
            }

            if (y + height > work.Y + work.Height)
            {
                y = work.Y + work.Height - height;
            }

            return new WindowBounds(x, y, width, height);
        }

        private static MonitorInfo ToMonitorInfo(Screen screen)
        {
            return new MonitorInfo(
                screen.DeviceName,
                FromRectangle(screen.Bounds),
                FromRectangle(screen.WorkingArea),
                screen.Primary);
        }

        private static WindowBounds FromRectangle(Rectangle rectangle)
        {
            return new WindowBounds(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }
    }
}
