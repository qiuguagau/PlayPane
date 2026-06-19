using PlayPane.Core.Services;

namespace PlayPane.Tests
{
    internal static class OverlayModePolicyTests
    {
        public static void EditModeDoesNotEnableOverlayBehavior()
        {
            var policy = OverlayModePolicy.ForEditMode();

            TestAssert.False(policy.IsTopmost, "Edit Mode should not force the mirror above other windows.");
            TestAssert.False(policy.IsClickThrough, "Edit Mode should keep the overlay interactive.");
        }

        public static void GameModeEnablesOverlayBehavior()
        {
            var policy = OverlayModePolicy.ForGameMode();

            TestAssert.True(policy.IsTopmost, "Game Mode should place the mirror above the game.");
            TestAssert.True(policy.IsClickThrough, "Game Mode should pass input through to the game.");
        }
    }
}
