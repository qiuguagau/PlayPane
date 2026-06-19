using System;

namespace PlayPane.Core.Capture
{
    public sealed class ExtensionFrameReceivedEventArgs : EventArgs
    {
        public ExtensionFrameReceivedEventArgs(int byteCount)
        {
            ByteCount = byteCount;
            ReceivedAtUtc = DateTime.UtcNow;
        }

        public int ByteCount { get; private set; }

        public DateTime ReceivedAtUtc { get; private set; }
    }
}
