using System;

namespace PlayPane.Core.Capture
{
    public sealed class WindowCaptureException : Exception
    {
        public WindowCaptureException(string message)
            : base(message)
        {
        }

        public WindowCaptureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
