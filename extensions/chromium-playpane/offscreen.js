let mediaStream = null;
let video = null;
let canvas = null;
let context = null;
let socket = null;
let frameTimer = null;
let frameQuality = 0.74;

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

  return false;
});

async function startCapture(options) {
  stopCapture();

  frameQuality = typeof options.quality === "number" ? options.quality : 0.74;
  mediaStream = await navigator.mediaDevices.getUserMedia({
    audio: false,
    video: {
      mandatory: {
        chromeMediaSource: "tab",
        chromeMediaSourceId: options.streamId
      }
    }
  });

  video = document.createElement("video");
  video.muted = true;
  video.playsInline = true;
  video.srcObject = mediaStream;
  await video.play();

  canvas = document.createElement("canvas");
  context = canvas.getContext("2d", { alpha: false });

  socket = new WebSocket(options.serverUrl);
  socket.binaryType = "arraybuffer";
  socket.addEventListener("open", () => {
    const frameRate = Math.max(5, Math.min(60, Number(options.frameRate) || 30));
    frameTimer = setInterval(sendFrame, Math.round(1000 / frameRate));
    reportStatus("capturing", "");
  });
  socket.addEventListener("close", () => reportStatus("disconnected", ""));
  socket.addEventListener("error", () => reportStatus("error", "Could not connect to the PlayPane desktop app."));

  const [track] = mediaStream.getVideoTracks();
  if (track) {
    track.addEventListener("ended", () => {
      stopCapture();
      reportStatus("idle", "");
    });
  }
}

function stopCapture() {
  if (frameTimer) {
    clearInterval(frameTimer);
    frameTimer = null;
  }

  if (socket) {
    try {
      socket.close();
    } catch {
      // Ignore close errors during teardown.
    }
    socket = null;
  }

  if (mediaStream) {
    for (const track of mediaStream.getTracks()) {
      track.stop();
    }
    mediaStream = null;
  }

  video = null;
  canvas = null;
  context = null;
}

function sendFrame() {
  if (!video || !canvas || !context || !socket || socket.readyState !== WebSocket.OPEN) {
    return;
  }

  if (!video.videoWidth || !video.videoHeight) {
    return;
  }

  if (canvas.width !== video.videoWidth || canvas.height !== video.videoHeight) {
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
  }

  context.drawImage(video, 0, 0, canvas.width, canvas.height);
  canvas.toBlob((blob) => {
    if (blob && socket && socket.readyState === WebSocket.OPEN) {
      socket.send(blob);
    }
  }, "image/jpeg", frameQuality);
}

function reportStatus(state, error) {
  chrome.runtime.sendMessage({
    target: "background",
    type: "capture-status",
    state,
    error
  });
}
