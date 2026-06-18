using System;
using System.IO;
using System.Runtime.Serialization.Json;
using PlayPane.Core.Models;

namespace PlayPane.Core.Services
{
    public sealed class RecoveryService
    {
        private readonly string _path;

        public RecoveryService()
            : this(DefaultRecoveryPath())
        {
        }

        public RecoveryService(string path)
        {
            _path = path;
        }

        public bool HasPendingSession
        {
            get { return File.Exists(_path); }
        }

        public WindowPlacementSnapshot Load()
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            try
            {
                using (var stream = File.OpenRead(_path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(WindowPlacementSnapshot));
                    return serializer.ReadObject(stream) as WindowPlacementSnapshot;
                }
            }
            catch
            {
                return null;
            }
        }

        public void Save(WindowPlacementSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            string directory = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = File.Create(_path))
            {
                var serializer = new DataContractJsonSerializer(typeof(WindowPlacementSnapshot));
                serializer.WriteObject(stream, snapshot);
            }
        }

        public void Clear()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }

        private static string DefaultRecoveryPath()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return System.IO.Path.Combine(local, "PlayPane", "pending-session.json");
        }
    }
}
