using System;
using PlayPane.Core.Models;
using PlayPane.Core.Services;
using PlayPane.Core.Windowing;

namespace PlayPane.Core.Application
{
    public sealed class SessionManager
    {
        private readonly StateManager _stateManager;
        private readonly SourceWindowManager _sourceWindowManager;
        private readonly RecoveryService _recoveryService;
        private readonly SettingsService _settingsService;
        private WindowPlacementSnapshot _sourceSnapshot;

        public SessionManager(StateManager stateManager, SourceWindowManager sourceWindowManager, RecoveryService recoveryService, SettingsService settingsService)
        {
            _stateManager = stateManager;
            _sourceWindowManager = sourceWindowManager;
            _recoveryService = recoveryService;
            _settingsService = settingsService;
        }

        public CaptureSession CurrentSession { get; private set; }

        public WindowPlacementSnapshot SourceSnapshot
        {
            get { return _sourceSnapshot; }
        }

        public CaptureSession Start(SourceWindowInfo source, AppSettings settings)
        {
            if (source == null)
            {
                throw new InvalidOperationException("Select a source window before starting mirroring.");
            }

            if (settings == null)
            {
                settings = AppSettings.CreateDefault();
            }

            settings.EnsureValid();
            _sourceSnapshot = _sourceWindowManager.CaptureSnapshot(source);
            _recoveryService.Save(_sourceSnapshot);
            _sourceWindowManager.ApplyPlacement(source, settings.SourcePlacement);

            settings.PreviousSource = new PreviousSourceWindow(source.ProcessName, source.BrowserType, source.Title, source.Bounds);
            _settingsService.Save(settings);

            CurrentSession = new CaptureSession(source, settings);

            if (_stateManager.Current == PlayPaneState.NoSourceSelected)
            {
                _stateManager.GoTo(PlayPaneState.Preview);
            }

            if (_stateManager.Current == PlayPaneState.Preview)
            {
                _stateManager.GoTo(PlayPaneState.Edit);
            }

            return CurrentSession;
        }

        public void Stop(bool restoreSource)
        {
            if (restoreSource && _sourceSnapshot != null)
            {
                _sourceWindowManager.Restore(_sourceSnapshot);
            }

            _recoveryService.Clear();
            CurrentSession = null;
            _sourceSnapshot = null;

            if (_stateManager.Current == PlayPaneState.Edit ||
                _stateManager.Current == PlayPaneState.Game ||
                _stateManager.Current == PlayPaneState.Paused ||
                _stateManager.Current == PlayPaneState.Error)
            {
                _stateManager.GoTo(PlayPaneState.Preview);
            }
        }

        public void RestoreSource()
        {
            if (_sourceSnapshot != null)
            {
                _sourceWindowManager.Restore(_sourceSnapshot);
                _recoveryService.Clear();
            }
        }
    }
}
