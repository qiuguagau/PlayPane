using System;
using PlayPane.Core.Models;

namespace PlayPane.Core.Services
{
    public sealed class FrameRateController
    {
        public int GetFramesPerSecond(FrameRateMode mode)
        {
            if (mode == FrameRateMode.LowResource)
            {
                return 15;
            }

            if (mode == FrameRateMode.Smooth)
            {
                return 60;
            }

            return 30;
        }

        public TimeSpan GetInterval(FrameRateMode mode)
        {
            return TimeSpan.FromMilliseconds(1000.0 / GetFramesPerSecond(mode));
        }
    }
}
