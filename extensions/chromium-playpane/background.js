const OFFSCREEN_DOCUMENT_PATH = "offscreen.html";
const PLAYPANE_SOCKET_URL = "ws://127.0.0.1:17632/playpane";

let captureState = {
  state: "idle",
  tabTitle: "",
  lastError: ""
};

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (!message || message.target === "offscreen") {
    return false;
  }

  if (message.target === "background" && message.type === "capture-status") {
    captureState = {
      state: message.state || "idle",
      tabTitle: captureState.tabTitle,
      lastError: message.error || ""
    };
    return false;
  }

  if (message.type === "start-capture") {
    startCapture(message)
      .then(() => sendResponse({ ok: true, state: captureState }))
      .catch((error) => {
        captureState = { state: "error", tabTitle: captureState.tabTitle, lastError: error.message };
        sendResponse({ ok: false, error: error.message, state: captureState });
      });
    return true;
  }

  if (message.type === "stop-capture") {
    stopCapture()
      .then(() => sendResponse({ ok: true, state: captureState }))
      .catch((error) => sendResponse({ ok: false, error: error.message, state: captureState }));
    return true;
  }

  if (message.type === "get-status") {
    sendResponse({ ok: true, state: captureState });
    return false;
  }

  return false;
});

async function startCapture(options) {
  if (!options || !options.streamId) {
    throw new Error("No tab capture stream id was provided.");
  }

  await ensureOffscreenDocument();

  captureState = {
    state: "starting",
    tabTitle: options.tabTitle || "Current tab",
    lastError: ""
  };

  await chrome.runtime.sendMessage({
    target: "offscreen",
    type: "start-capture",
    streamId: options.streamId,
    serverUrl: PLAYPANE_SOCKET_URL,
    frameRate: 30
  });

  captureState = {
    state: "capturing",
    tabTitle: options.tabTitle || "Current tab",
    lastError: ""
  };
}

async function stopCapture() {
  await ensureOffscreenDocument();
  await chrome.runtime.sendMessage({ target: "offscreen", type: "stop-capture" });
  captureState = {
    state: "idle",
    tabTitle: captureState.tabTitle,
    lastError: ""
  };
}

async function ensureOffscreenDocument() {
  if (await chrome.offscreen.hasDocument()) {
    return;
  }

  await chrome.offscreen.createDocument({
    url: OFFSCREEN_DOCUMENT_PATH,
    reasons: ["USER_MEDIA"],
    justification: "Capture the user-selected browser tab and stream it to the PlayPane desktop app."
  });
}
