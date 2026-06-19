# PlayPane Capture Extension

This unpacked Chromium extension captures the current Chrome or Microsoft Edge tab and streams JPEG frames to the PlayPane desktop app.

## Install

1. Open Chrome or Edge.
2. Navigate to `chrome://extensions` or `edge://extensions`.
3. Enable Developer mode.
4. Choose Load unpacked.
5. Select this folder: `extensions/chromium-playpane`.

## Use

1. Start PlayPane.
2. Set Capture source to Chrome/Edge Extension Capture.
3. Click Start Overlay.
4. Open the target browser tab.
5. Click the PlayPane Capture extension icon.
6. Click Start current tab.

The desktop app listens on `ws://127.0.0.1:17632/playpane`.
