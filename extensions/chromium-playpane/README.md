# PlayPane Capture Extension

This unpacked Chromium extension captures the current Chrome or Microsoft Edge tab and streams it to the PlayPane desktop app over WebRTC.

## Install

1. Open Chrome or Edge.
2. Navigate to `chrome://extensions` or `edge://extensions`.
3. Enable Developer mode.
4. Choose Load unpacked.
5. Select this folder: `extensions/chromium-playpane`.

## Use

1. Start PlayPane.
2. Open the target browser tab.
3. Click the PlayPane Capture extension icon.
4. Click Start current tab.
5. Wait for the stream to appear in the PlayPane preview panel.
6. Click Start Overlay when you want the floating mirror window.

The desktop app listens on `ws://127.0.0.1:17632/playpane` for local WebRTC signaling. Video frames are carried by WebRTC, not by WebSocket.

After a tab is selected once, PlayPane can stop and start the overlay without requiring another Start current tab click. When the overlay stops, the desktop app returns to the main preview viewer. Click Stop in this popup when you want to release the selected tab capture.
