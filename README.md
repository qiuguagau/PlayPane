# PlayPane

[简体中文](README.zh-CN.md)

PlayPane is a Windows desktop utility for mirroring a Chrome or Microsoft Edge tab into an always-on-top overlay for windowed or borderless fullscreen games.

## Current MVP

- Captures a Chrome or Microsoft Edge tab through the unpacked extension in `extensions/chromium-playpane`.
- Starts the local WebRTC signaling server when the app opens and shows the extension stream in the main preview panel when available.
- Starts a movable, resizable, always-on-top overlay backed by an embedded WebView2 WebRTC viewer.
- Supports opacity control, 15/30/60 FPS modes, Edit Mode, Game Mode, and mouse click-through.
- Registers default global shortcuts:
  - `Ctrl + Alt + O`: show/hide overlay
  - `Ctrl + Alt + E`: toggle Edit/Game Mode
  - `Ctrl + Alt + Up`: increase opacity
  - `Ctrl + Alt + Down`: decrease opacity
  - `Ctrl + Alt + Q`: stop mirroring
- Saves local settings under the current user's application data folder.
- Supports English and Simplified Chinese UI language selection from Settings.
- Uses WebRTC for video mirroring while the local WebSocket is used only for signaling.
- Provides a system tray menu for common actions.

## Build

Requires Windows and the .NET SDK with WPF support.

```powershell
dotnet build PlayPane.sln
dotnet run --project tests\PlayPane.Tests\PlayPane.Tests.csproj
dotnet run --project src\PlayPane\PlayPane.csproj -- --smoke-test
dotnet run --project src\PlayPane\PlayPane.csproj
```

## Chrome/Edge Extension Capture

1. Build and start PlayPane.
2. PlayPane starts a local WebRTC signaling server at `ws://127.0.0.1:17632/playpane` and waits in the main preview panel.
3. In Chrome or Edge, open `chrome://extensions` or `edge://extensions`.
4. Enable Developer mode and load the unpacked extension folder `extensions/chromium-playpane`.
5. Open the target tab, click the PlayPane Capture extension icon, then click Start current tab.
6. After the preview appears in PlayPane, click Start Overlay.

The extension captures the current tab with Chrome's `tabCapture` API and sends the video stream over WebRTC to the overlay's embedded WebView2 viewer. The local WebSocket only exchanges control and signaling messages such as `viewer-ready`, `viewer-stopped`, `offer`, `answer`, and ICE candidates.

After the tab is selected once, stopping and starting the PlayPane overlay reconnects to the same extension source automatically. Use Stop in the extension popup only when you want to release the selected browser tab.
