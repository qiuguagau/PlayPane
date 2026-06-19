using System;
using System.Drawing;
using System.IO;

namespace PlayPane.Core.Capture
{
    public sealed class ExtensionFrameStore
    {
        private readonly object _syncRoot = new object();
        private byte[] _latestFrameBytes;
        private DateTime _lastUpdatedUtc;

        public DateTime LastUpdatedUtc
        {
            get
            {
                lock (_syncRoot)
                {
                    return _lastUpdatedUtc;
                }
            }
        }

        public bool HasFrame
        {
            get
            {
                lock (_syncRoot)
                {
                    return _latestFrameBytes != null && _latestFrameBytes.Length > 0;
                }
            }
        }

        public void Update(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return;
            }

            lock (_syncRoot)
            {
                _latestFrameBytes = (byte[])imageBytes.Clone();
                _lastUpdatedUtc = DateTime.UtcNow;
            }
        }

        public bool TryGetFrame(out Bitmap bitmap)
        {
            byte[] bytes;
            lock (_syncRoot)
            {
                if (_latestFrameBytes == null || _latestFrameBytes.Length == 0)
                {
                    bitmap = null;
                    return false;
                }

                bytes = (byte[])_latestFrameBytes.Clone();
            }

            using (var stream = new MemoryStream(bytes))
            using (var decoded = new Bitmap(stream))
            {
                bitmap = new Bitmap(decoded);
                return true;
            }
        }
    }
}
