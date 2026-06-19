const statusElement = document.getElementById("status");
const startButton = document.getElementById("startButton");
const stopButton = document.getElementById("stopButton");

startButton.addEventListener("click", async () => {
  try {
    setStatus("Starting...");
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
    if (!tab || !tab.id) {
      throw new Error("No active tab is available.");
    }

    const streamId = await getMediaStreamId(tab.id);
    const response = await chrome.runtime.sendMessage({
      type: "start-capture",
      streamId,
      tabTitle: tab.title || "Current tab"
    });

    if (response && response.ok) {
      renderState(response.state);
    } else {
      setStatus(response && response.error ? response.error : "Could not start capture.");
    }
  } catch (error) {
    setStatus(error.message || "Could not start capture.");
  }
});

stopButton.addEventListener("click", async () => {
  const response = await chrome.runtime.sendMessage({ type: "stop-capture" });
  if (response && response.ok) {
    renderState(response.state);
  } else {
    setStatus(response && response.error ? response.error : "Could not stop capture.");
  }
});

chrome.runtime.sendMessage({ type: "get-status" }, (response) => {
  if (response && response.ok) {
    renderState(response.state);
  }
});

function renderState(state) {
  if (!state) {
    setStatus("Idle");
    return;
  }

  if (state.state === "capturing") {
    setStatus("Capturing: " + (state.tabTitle || "current tab"));
    return;
  }

  if (state.state === "starting") {
    setStatus("Starting: " + (state.tabTitle || "current tab"));
    return;
  }

  if (state.state === "error") {
    setStatus(state.lastError || "Capture error.");
    return;
  }

  if (state.state === "disconnected") {
    setStatus("Disconnected from PlayPane desktop app.");
    return;
  }

  setStatus("Idle");
}

function setStatus(text) {
  statusElement.textContent = text;
}

function getMediaStreamId(tabId) {
  return new Promise((resolve, reject) => {
    chrome.tabCapture.getMediaStreamId({ targetTabId: tabId }, (streamId) => {
      const error = chrome.runtime.lastError;
      if (error) {
        reject(new Error(error.message));
        return;
      }

      if (!streamId) {
        reject(new Error("Chrome did not return a tab capture stream id."));
        return;
      }

      resolve(streamId);
    });
  });
}
