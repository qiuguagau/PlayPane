let mediaStream = null;
let socket = null;
let peerConnection = null;
let pendingRemoteCandidates = [];
let captureOptions = null;
let reconnectTimer = null;
let intentionallyStopping = false;

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (!message || message.target !== "offscreen") {
    return false;
  }

  if (message.type === "start-capture") {
    startCapture(message)
      .then(() => sendResponse({ ok: true }))
      .catch((error) => {
        reportStatus("error", error.message);
        sendResponse({ ok: false, error: error.message });
      });
    return true;
  }

  if (message.type === "stop-capture") {
    stopCapture();
    sendResponse({ ok: true });
    return false;
  }

  if (message.type === "desktop-exiting") {
    stopCapture();
    reportStatus("idle", "");
    sendResponse({ ok: true });
    return false;
  }

  return false;
});

async function startCapture(options) {
  stopCapture();
  intentionallyStopping = false;

  captureOptions = {
    serverUrl: options.serverUrl,
    frameRate: Math.max(5, Math.min(60, Number(options.frameRate) || 30))
  };

  mediaStream = await navigator.mediaDevices.getUserMedia({
    audio: false,
    video: {
      mandatory: {
        chromeMediaSource: "tab",
        chromeMediaSourceId: options.streamId,
        maxFrameRate: captureOptions.frameRate
      }
    }
  });

  const [track] = mediaStream.getVideoTracks();
  if (track && typeof track.applyConstraints === "function") {
    try {
      await track.applyConstraints({ frameRate: { max: captureOptions.frameRate } });
    } catch {
      // Tab capture may ignore frame-rate constraints; WebRTC still adapts.
    }

    track.addEventListener("ended", () => {
      stopCapture();
      reportStatus("idle", "");
    });
  }

  try {
    await connectSignaling();
    reportStatus("capturing", "");
  } catch (error) {
    stopCapture();
    throw error;
  }
}

function stopCapture() {
  intentionallyStopping = true;
  clearReconnectTimer();
  stopPeerConnection();
  closeSocket();

  if (mediaStream) {
    for (const track of mediaStream.getTracks()) {
      track.stop();
    }
    mediaStream = null;
  }

  captureOptions = null;
  pendingRemoteCandidates = [];
}

function stopPeerConnection() {
  pendingRemoteCandidates = [];

  if (peerConnection) {
    try {
      peerConnection.close();
    } catch {
      // Ignore close errors during teardown.
    }
    peerConnection = null;
  }
}

function closeSocket() {
  const currentSocket = socket;
  socket = null;

  if (currentSocket) {
    try {
      currentSocket.close();
    } catch {
      // Ignore close errors during teardown.
    }
  }
}

async function connectSignaling() {
  if (!captureOptions || intentionallyStopping) {
    return;
  }

  if (socket && socket.readyState === WebSocket.OPEN) {
    sendSignal({ role: "source", type: "hello" });
    return;
  }

  if (socket && socket.readyState === WebSocket.CONNECTING) {
    await waitForSocketOpen(socket);
    sendSignal({ role: "source", type: "hello" });
    return;
  }

  const nextSocket = new WebSocket(captureOptions.serverUrl);
  socket = nextSocket;

  nextSocket.addEventListener("message", (event) => {
    handleSignal(event.data).catch((error) => reportStatus("error", error.message));
  });
  nextSocket.addEventListener("close", () => {
    if (socket === nextSocket) {
      socket = null;
    }

    stopPeerConnection();
    if (!intentionallyStopping && mediaStream) {
      reportStatus("disconnected", "");
      scheduleReconnect();
    }
  });
  nextSocket.addEventListener("error", () => {
    if (!intentionallyStopping) {
      reportStatus("error", "Could not connect to the PlayPane desktop app.");
    }
  });

  await waitForSocketOpen(nextSocket);
  if (socket === nextSocket) {
    sendSignal({ role: "source", type: "hello" });
  }
}

function scheduleReconnect() {
  if (reconnectTimer || intentionallyStopping || !mediaStream || !captureOptions) {
    return;
  }

  reconnectTimer = setTimeout(() => {
    reconnectTimer = null;
    connectSignaling().catch(() => scheduleReconnect());
  }, 1000);
}

function clearReconnectTimer() {
  if (reconnectTimer) {
    clearTimeout(reconnectTimer);
    reconnectTimer = null;
  }
}

async function startPeerConnection() {
  if (!mediaStream || !socket || socket.readyState !== WebSocket.OPEN) {
    return;
  }

  stopPeerConnection();
  peerConnection = createPeerConnection();
  for (const streamTrack of mediaStream.getTracks()) {
    const sender = addStreamTrack(streamTrack);
    if (streamTrack.kind === "video") {
      await applySenderFrameRate(sender);
    }
  }

  const offer = await peerConnection.createOffer();
  if (!peerConnection) {
    return;
  }

  await peerConnection.setLocalDescription(offer);
  sendSignal({ role: "source", type: "offer", sdp: peerConnection.localDescription.sdp });
  reportStatus("capturing", "");
}

function addStreamTrack(streamTrack) {
  if (streamTrack.kind === "video" && typeof peerConnection.addTransceiver === "function") {
    const transceiver = peerConnection.addTransceiver(streamTrack, {
      direction: "sendonly",
      streams: [mediaStream],
      sendEncodings: [{ maxFramerate: captureOptions.frameRate }]
    });

    return transceiver.sender;
  }

  return peerConnection.addTrack(streamTrack, mediaStream);
}

function createPeerConnection() {
  const connection = new RTCPeerConnection({ iceServers: [] });
  connection.addEventListener("icecandidate", (event) => {
    if (event.candidate) {
      sendSignal({ role: "source", type: "ice", candidate: event.candidate.toJSON() });
    }
  });
  connection.addEventListener("connectionstatechange", () => {
    if (!peerConnection || connection !== peerConnection) {
      return;
    }

    if (connection.connectionState === "failed" || connection.connectionState === "disconnected") {
      stopPeerConnection();
      reportStatus("disconnected", "");
    }
  });
  return connection;
}

async function handleSignal(rawMessage) {
  if (!rawMessage) {
    return;
  }

  const message = JSON.parse(rawMessage);
  if (message.role !== "viewer") {
    return;
  }

  if (message.type === "viewer-ready") {
    await applyRequestedFrameRate(message.frameRate);
    await startPeerConnection();
    return;
  }

  if (message.type === "viewer-stopped") {
    stopPeerConnection();
    return;
  }

  if (message.type === "desktop-exiting") {
    stopCapture();
    reportStatus("idle", "");
    return;
  }

  if (!peerConnection) {
    return;
  }

  if (message.type === "answer" && message.sdp) {
    await peerConnection.setRemoteDescription({ type: "answer", sdp: message.sdp });
    await flushPendingRemoteCandidates();
    return;
  }

  if (message.type === "ice" && message.candidate) {
    if (peerConnection.remoteDescription) {
      await peerConnection.addIceCandidate(message.candidate);
    } else {
      pendingRemoteCandidates.push(message.candidate);
    }
  }
}

async function flushPendingRemoteCandidates() {
  while (pendingRemoteCandidates.length > 0 && peerConnection && peerConnection.remoteDescription) {
    const candidate = pendingRemoteCandidates.shift();
    await peerConnection.addIceCandidate(candidate);
  }
}

async function applyRequestedFrameRate(frameRate) {
  if (!mediaStream) {
    return;
  }

  const requestedFrameRate = Math.max(5, Math.min(60, Number(frameRate) || captureOptions.frameRate || 60));
  captureOptions.frameRate = requestedFrameRate;
  const [track] = mediaStream.getVideoTracks();
  if (track && typeof track.applyConstraints === "function") {
    try {
      await track.applyConstraints({ frameRate: { max: requestedFrameRate } });
    } catch {
      // Tab capture may ignore frame-rate constraints; WebRTC still adapts.
    }
  }

  if (peerConnection) {
    for (const sender of peerConnection.getSenders()) {
      if (sender.track && sender.track.kind === "video") {
        await applySenderFrameRate(sender);
      }
    }
  }
}

async function applySenderFrameRate(sender) {
  if (!sender || typeof sender.getParameters !== "function" || typeof sender.setParameters !== "function" || !captureOptions) {
    return;
  }

  const parameters = sender.getParameters();
  if (!parameters.encodings || parameters.encodings.length === 0) {
    parameters.encodings = [{}];
  }

  for (const encoding of parameters.encodings) {
    encoding.maxFramerate = captureOptions.frameRate;
  }

  try {
    await sender.setParameters(parameters);
  } catch {
    // Some Chromium builds reject sender parameter changes for tab capture.
  }
}

function sendSignal(message) {
  if (socket && socket.readyState === WebSocket.OPEN) {
    socket.send(JSON.stringify(message));
  }
}

function waitForSocketOpen(targetSocket) {
  return new Promise((resolve, reject) => {
    if (targetSocket.readyState === WebSocket.OPEN) {
      resolve();
      return;
    }

    const timeoutId = setTimeout(() => {
      reject(new Error("Timed out connecting to the PlayPane desktop app."));
    }, 5000);

    targetSocket.addEventListener("open", () => {
      clearTimeout(timeoutId);
      resolve();
    }, { once: true });

    targetSocket.addEventListener("error", () => {
      clearTimeout(timeoutId);
      reject(new Error("Could not connect to the PlayPane desktop app."));
    }, { once: true });
  });
}

function reportStatus(state, error) {
  chrome.runtime.sendMessage({
    target: "background",
    type: "capture-status",
    state,
    error
  });
}
