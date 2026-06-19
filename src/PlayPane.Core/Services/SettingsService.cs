using System;
using System.IO;
using System.Runtime.Serialization.Json;
using PlayPane.Core.Models;

namespace PlayPane.Core.Services
{
    public sealed class SettingsService
    {
        private readonly string _path;

        public SettingsService()
            : this(DefaultSettingsPath())
        {
        }

        public SettingsService(string path)
        {
            _path = path;
        }

        public string Path
        {
            get { return _path; }
        }

        public AppSettings Load()
        {
            if (!File.Exists(_path))
            {
                return AppSettings.CreateDefault();
            }

            try
            {
                using (var stream = File.OpenRead(_path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                    var settings = serializer.ReadObject(stream) as AppSettings;
                    if (settings == null)
                    {
                        return AppSettings.CreateDefault();
                    }

                    settings.EnsureValid();
                    return settings;
                }
            }
            catch
            {
                return AppSettings.CreateDefault();
            }
        }

        public void Save(AppSettings settings)
        {
            if (settings == null)
            {
                settings = AppSettings.CreateDefault();
            }

            settings.EnsureValid();

            string directory = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = File.Create(_path))
            {
                var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                serializer.WriteObject(stream, settings);
            }
        }

        private static string DefaultSettingsPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return System.IO.Path.Combine(appData, "PlayPane", "settings.json");
        }
    }
}
