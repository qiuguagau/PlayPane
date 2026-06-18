# PlayPane

PlayPane is a Windows desktop utility for mirroring an existing browser window into an always-on-top overlay for windowed or borderless fullscreen games.

## Current MVP

- Lists capturable Chrome, Firefox, Edge, and optionally all top-level windows.
- Starts a movable, resizable, always-on-top overlay.
- Captures the selected window with a Win32 `PrintWindow` backend and a screen-copy fallback.
- Supports full-window mirroring and rectangular crop selection.
- Supports opacity control, 15/30/60 FPS modes, Edit Mode, Game Mode, and mouse click-through.
- Registers default global shortcuts:
  - `Ctrl + Alt + O`: show/hide overlay
  - `Ctrl + Alt + E`: toggle Edit/Game Mode
  - `Ctrl + Alt + Up`: increase opacity
  - `Ctrl + Alt + Down`: decrease opacity
  - `Ctrl + Alt + R`: edit crop
  - `Ctrl + Alt + Q`: stop mirroring
- Saves local settings under the current user's application data folder.
- Supports English and Simplified Chinese UI language selection from Settings.
- Saves a temporary recovery file before moving the source window and offers recovery on next launch.
- Provides a system tray menu for common actions.

## Build

Requires Windows and the .NET SDK with WPF support.

```powershell
dotnet build PlayPane.sln
dotnet run --project tests\PlayPane.Tests\PlayPane.Tests.csproj
dotnet run --project src\PlayPane\PlayPane.csproj -- --smoke-test
dotnet run --project src\PlayPane\PlayPane.csproj
```

## Notes

The product spec recommends Windows Graphics Capture for the final capture backend. This MVP keeps the capture backend isolated behind `IWindowCaptureService`; the current implementation uses Win32 `PrintWindow` plus fallback screen copy so the rest of the app is usable while the lower-level capture backend can be upgraded later.
