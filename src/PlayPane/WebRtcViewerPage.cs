using System;
using System.Globalization;
using System.Text.Json;

namespace PlayPane
{
    internal static class WebRtcViewerPage
    {
        public static string Build(Uri signalingUri, string waitingText, bool aspectRatioLocked, int opacityPercent, int frameRate)
        {
            string signalingUrl = JsonSerializer.Serialize(signalingUri.ToString());
            string serializedWaitingText = JsonSerializer.Serialize(waitingText);
            string objectFit = aspectRatioLocked ? "contain" : "fill";
            string initialOpacity = (Math.Max(10, Math.Min(100, opacityPercent)) / 100.0).ToString(CultureInfo.InvariantCulture);
            int requestedFrameRate = Math.Max(5, Math.Min(60, frameRate));

            return @"<!doctype html>
<html>
<head>
  <meta charset=""utf-8"">
  <meta http-equiv=""Content-Security-Policy"" content=""default-src 'none'; connect-src ws://127.0.0.1:*; style-src 'unsafe-inline'; script-src 'unsafe-inline'; media-src blob: mediastream:"">
  <style>
    html, body {
      margin: 0;
      width: 100%;
      height: 100%;
      overflow: hidden;
      background: #020617;
    }
    :root {
      --playpane-opacity: " + initialOpacity + @";
    }
    video {
      width: 100vw;
      height: 100vh;
      object-fit: " + objectFit + @";
      opacity: var(--playpane-opacity);
      background: #020617;
      display: block;
    }
    #status {
      position: fixed;
      left: 0;
      right: 0;
      top: 45%;
      padding: 0 18px;
      color: #cbd5e1;
      font: 14px/1.5 system-ui, -apple-system, BlinkMacSystemFont, ""Segoe UI"", sans-serif;
      text-align: center;
    }
  </style>
</head>
<body>
  <video id=""video"" autoplay muted playsinline></video>
  <div id=""status""></div>
  <script>
    const signalingUrl = " + signalingUrl + @";
    const waitingText = " + serializedWaitingText + @";
    const requestedFrameRate = " + requestedFrameRate.ToString(CultureInfo.InvariantCulture) + @";
    const video = document.getElementById('video');
    const status = document.getElementById('status');
    let socket;
    let peerConnection;
    let viewerStopped = false;
    const pendingCandidates = [];

    status.textContent = waitingText;
    connect();

    function connect() {
      peerConnection = createPeerConnection();
      socket = new WebSocket(signalingUrl);
      socket.addEventListener('open', () => {
        viewerStopped = false;
        sendSignal({ role: 'viewer', type: 'hello' });
        sendSignal({ role: 'viewer', type: 'viewer-ready', frameRate: requestedFrameRate });
      });
      socket.addEventListener('message', event => handleSignal(event.data).catch(showError));
      socket.addEventListener('close', () => {
        if (!video.srcObject) {
          status.hidden = false;
          status.textContent = waitingText;
        }
      });
      socket.addEventListener('error', () => showError(new Error('Could not connect to PlayPane signaling.')));
    }

    function createPeerConnection() {
      const connection = new RTCPeerConnection({ iceServers: [] });
      connection.addEventListener('track', event => {
        if (event.streams && event.streams[0]) {
          video.srcObject = event.streams[0];
        } else {
          const stream = video.srcObject instanceof MediaStream ? video.srcObject : new MediaStream();
          stream.addTrack(event.track);
          video.srcObject = stream;
        }

        status.hidden = true;
        video.play().catch(() => {});
      });
      connection.addEventListener('icecandidate', event => {
        if (event.candidate) {
          sendSignal({ role: 'viewer', type: 'ice', candidate: event.candidate.toJSON() });
        }
      });
      connection.addEventListener('connectionstatechange', () => {
        if (connection.connectionState === 'failed') {
          status.hidden = false;
          status.textContent = 'WebRTC connection failed.';
        }
      });
      return connection;
    }

    function resetPeerConnection() {
      if (peerConnection) {
        try {
          peerConnection.close();
        } catch {
        }
      }

      pendingCandidates.length = 0;
      peerConnection = createPeerConnection();
    }

    async function handleSignal(rawMessage) {
      if (!rawMessage) {
        return;
      }

      const message = JSON.parse(rawMessage);
      if (message.role !== 'source') {
        return;
      }

      if (message.type === 'offer' && message.sdp) {
        resetPeerConnection();
        await peerConnection.setRemoteDescription({ type: 'offer', sdp: message.sdp });
        const answer = await peerConnection.createAnswer();
        await peerConnection.setLocalDescription(answer);
        sendSignal({ role: 'viewer', type: 'answer', sdp: peerConnection.localDescription.sdp });
        await flushPendingCandidates();
        return;
      }

      if (message.type === 'ice' && message.candidate) {
        if (peerConnection.remoteDescription) {
          await peerConnection.addIceCandidate(message.candidate);
        } else {
          pendingCandidates.push(message.candidate);
        }
      }
    }

    async function flushPendingCandidates() {
      while (pendingCandidates.length > 0 && peerConnection.remoteDescription) {
        await peerConnection.addIceCandidate(pendingCandidates.shift());
      }
    }

    function sendSignal(message) {
      if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify(message));
      }
    }

    function stopViewer() {
      if (viewerStopped) {
        return;
      }

      viewerStopped = true;
      sendSignal({ role: 'viewer', type: 'viewer-stopped' });
      if (peerConnection) {
        try {
          peerConnection.close();
        } catch {
        }
        peerConnection = null;
      }

      video.srcObject = null;
      setTimeout(() => {
        if (socket) {
          try {
            socket.close();
          } catch {
          }
          socket = null;
        }
      }, 50);
    }

    function showError(error) {
      status.hidden = false;
      status.textContent = error && error.message ? error.message : 'WebRTC error.';
    }

    window.playPaneStopViewer = stopViewer;
    window.addEventListener('beforeunload', stopViewer);
  </script>
</body>
</html>";
        }

        public static string Blank
        {
            get { return "<!doctype html><html><body style=\"margin:0;background:#020617\"></body></html>"; }
        }
    }
}
