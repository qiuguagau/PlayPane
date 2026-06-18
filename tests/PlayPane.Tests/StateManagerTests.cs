using PlayPane.Core.Models;
using PlayPane.Core.Services;

namespace PlayPane.Tests
{
    internal static class StateManagerTests
    {
        public static void AllowsExpectedStartupFlow()
        {
            var manager = new StateManager();

            manager.GoTo(PlayPaneState.Preview);
            manager.GoTo(PlayPaneState.Edit);
            manager.GoTo(PlayPaneState.Game);
            manager.GoTo(PlayPaneState.Paused);
            manager.GoTo(PlayPaneState.Game);

            TestAssert.Equal(PlayPaneState.Game, manager.Current, "Expected active game state.");
        }

        public static void RejectsInvalidTransition()
        {
            var manager = new StateManager();
            var accepted = manager.TryGoTo(PlayPaneState.Game);

            TestAssert.False(accepted, "Cannot enter game mode without selecting and starting a source.");
            TestAssert.Equal(PlayPaneState.NoSourceSelected, manager.Current, "Invalid transition should not change state.");
        }
    }
}
