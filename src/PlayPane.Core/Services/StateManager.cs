using System;
using PlayPane.Core.Models;

namespace PlayPane.Core.Services
{
    public sealed class StateManager
    {
        public StateManager()
        {
            Current = PlayPaneState.NoSourceSelected;
        }

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public PlayPaneState Current { get; private set; }

        public void GoTo(PlayPaneState target)
        {
            if (!TryGoTo(target))
            {
                throw new InvalidOperationException("Invalid PlayPane state transition from " + Current + " to " + target + ".");
            }
        }

        public bool TryGoTo(PlayPaneState target)
        {
            if (!CanTransition(Current, target))
            {
                return false;
            }

            if (Current == target)
            {
                return true;
            }

            PlayPaneState previous = Current;
            Current = target;
            OnStateChanged(previous, target);
            return true;
        }

        public static bool CanTransition(PlayPaneState current, PlayPaneState target)
        {
            if (current == target)
            {
                return true;
            }

            if (target == PlayPaneState.Error)
            {
                return current == PlayPaneState.Preview ||
                    current == PlayPaneState.Edit ||
                    current == PlayPaneState.Game ||
                    current == PlayPaneState.Paused;
            }

            switch (current)
            {
                case PlayPaneState.NoSourceSelected:
                    return target == PlayPaneState.Preview;
                case PlayPaneState.Preview:
                    return target == PlayPaneState.NoSourceSelected ||
                        target == PlayPaneState.Edit;
                case PlayPaneState.Edit:
                    return target == PlayPaneState.Preview ||
                        target == PlayPaneState.NoSourceSelected ||
                        target == PlayPaneState.Game ||
                        target == PlayPaneState.Paused;
                case PlayPaneState.Game:
                    return target == PlayPaneState.Edit ||
                        target == PlayPaneState.Paused;
                case PlayPaneState.Paused:
                    return target == PlayPaneState.Game ||
                        target == PlayPaneState.Edit;
                case PlayPaneState.Error:
                    return target == PlayPaneState.Preview ||
                        target == PlayPaneState.NoSourceSelected;
                default:
                    return false;
            }
        }

        private void OnStateChanged(PlayPaneState previous, PlayPaneState current)
        {
            EventHandler<StateChangedEventArgs> handler = StateChanged;
            if (handler != null)
            {
                handler(this, new StateChangedEventArgs(previous, current));
            }
        }
    }
}
