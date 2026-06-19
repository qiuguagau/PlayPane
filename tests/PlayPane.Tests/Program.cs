using System;
using System.Collections.Generic;

namespace PlayPane.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            var tests = new List<Action>
            {
                CropRegionTests.FullRegionCoversSource,
                CropRegionTests.PixelRegionScalesWithSourceSize,
                CropRegionTests.ClampKeepsRegionInsideSource,
                SettingsServiceTests.SaveAndLoadRoundTrip,
                StateManagerTests.AllowsExpectedStartupFlow,
                StateManagerTests.RejectsInvalidTransition,
                WindowClassifierTests.ClassifiesSupportedBrowsers,
                WindowClassifierTests.FiltersDefaultWindowList,
                LocalizationServiceTests.UsesEnglishByDefault,
                LocalizationServiceTests.TranslatesSimplifiedChinese,
                LocalizationServiceTests.FallsBackToEnglishForUnknownKey,
                SettingsServiceTests.LanguageRoundTrips,
                SettingsServiceTests.DefaultsToBrowserExtensionCapture,
                OverlayModePolicyTests.EditModeDoesNotEnableOverlayBehavior,
                OverlayModePolicyTests.GameModeEnablesOverlayBehavior,
                OverlayWindowLayoutTests.ToolbarUsesWrappingLayout,
                OverlayTransparencyTests.OverlayWindowAllowsDesktopTransparency,
                OverlayTransparencyTests.WebRtcViewerUsesTransparentBackingSurfaces,
                CaptureSessionTests.BrowserExtensionSessionDoesNotRequireSourceWindow,
                ExtensionSignalingServerTests.RoutesOfferFromSourceToViewer,
                ExtensionSignalingServerTests.QueuesOfferUntilViewerConnects,
                ExtensionSignalingServerTests.RoutesViewerReadyFromViewerToSource,
                ExtensionSignalingServerTests.QueuesViewerReadyUntilSourceConnects,
                ExtensionSignalingServerTests.RequestsSourceCaptureStopOnDesktopExit,
                ExtensionOffscreenScriptTests.DesktopExitSignalStopsTabCapture,
                ApplicationExitTests.ExitRequestsExtensionCaptureStopBeforeStoppingServer,
                TrayOverlayMenuTests.TrayOverlayItemsUseDedicatedCommands,
                TrayOverlayMenuTests.TrayShowOverlayBringsExistingOverlayForward,
                TrayOverlayMenuTests.TrayHideOverlayUpdatesState,
                TrayOverlayMenuTests.TrayModeItemsUseDedicatedCommands,
                TrayOverlayMenuTests.TrayEnterEditModeBringsLockedOverlayForward,
                TrayOverlayMenuTests.TrayEditForegroundHelperUsesWin32ZOrder,
                TrayOverlayMenuTests.Win32ApiExposesForegroundZOrderPrimitives,
                TrayOverlayMenuTests.TrayMenuUsesNotifyIconNativeContextMenu,
                TrayOverlayMenuTests.TrayMenuOpeningRefreshesMenuState,
                SessionManagerTests.StopBrowserExtensionSessionIsIdempotent
            };

            var failures = 0;
            foreach (var test in tests)
            {
                try
                {
                    test();
                    Console.WriteLine("PASS " + test.Method.DeclaringType.Name + "." + test.Method.Name);
                }
                catch (Exception ex)
                {
                    failures++;
                    Console.Error.WriteLine("FAIL " + test.Method.DeclaringType.Name + "." + test.Method.Name);
                    Console.Error.WriteLine(ex);
                }
            }

            Console.WriteLine();
            Console.WriteLine((tests.Count - failures) + "/" + tests.Count + " tests passed");
            return failures == 0 ? 0 : 1;
        }
    }
}
