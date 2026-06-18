using System;
using PlayPane.Core.Models;

namespace PlayPane.Core.Services
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public StateChangedEventArgs(PlayPaneState previous, PlayPaneState current)
        {
            Previous = previous;
            Current = current;
        }

        public PlayPaneState Previous { get; private set; }

        public PlayPaneState Current { get; private set; }
    }
}
