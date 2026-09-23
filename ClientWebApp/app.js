const apiUrlInput = document.querySelector("#apiUrl");
const settingsButton = document.querySelector("#settingsButton");
const settingsPanel = document.querySelector("#settingsPanel");
const saveSettingsButton = document.querySelector("#saveSettingsButton");
const chatForm = document.querySelector("#chatForm");
const messageInput = document.querySelector("#messageInput");
const conversation = document.querySelector("#conversation");
const sendButton = document.querySelector(".send-button");
const connectionStatus = document.querySelector("#connectionStatus");
const statusDot = document.querySelector("#statusDot");

const storedApiUrl = localStorage.getItem("aiAgentApiUrl");
if (storedApiUrl) apiUrlInput.value = storedApiUrl;

settingsButton.addEventListener("click", () => {
  const isHidden = settingsPanel.hidden;
  settingsPanel.hidden = !isHidden;
  settingsButton.setAttribute("aria-expanded", String(isHidden));
  if (isHidden) apiUrlInput.focus();
});

saveSettingsButton.addEventListener("click", () => {
  const apiUrl = getApiUrl();
  localStorage.setItem("aiAgentApiUrl", apiUrl);
  setConnection("ready", "Endpoint saved");
  settingsPanel.hidden = true;
  settingsButton.setAttribute("aria-expanded", "false");
});

document.querySelectorAll(".suggestion").forEach((button) => {
  button.addEventListener("click", () => {
    messageInput.value = button.dataset.prompt;
    resizeInput();
    messageInput.focus();
  });
});

chatForm.addEventListener("submit", (event) => {
  event.preventDefault();
  sendMessage();
});

messageInput.addEventListener("keydown", (event) => {
  if (event.key === "Enter" && !event.shiftKey) {
    event.preventDefault();
    sendMessage();
  }
});

messageInput.addEventListener("input", resizeInput);

function getApiUrl() {
  return apiUrlInput.value.trim().replace(/\/$/, "");
}

function resizeInput() {
  messageInput.style.height = "auto";
  messageInput.style.height = `${Math.min(messageInput.scrollHeight, 130)}px`;
}

async function sendMessage() {
  const message = messageInput.value.trim();
  if (!message || sendButton.disabled) return;

  addUserMessage(message);
  messageInput.value = "";
  resizeInput();
  setLoading(true);
  const typingMessage = addTypingMessage();

  try {
    const response = await fetch(`${getApiUrl()}/api/agent`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ message }),
    });

    const payload = await response.json().catch(() => null);
    if (!response.ok) {
      throw new Error(
        payload?.message || `Agent service returned ${response.status}.`,
      );
    }

    typingMessage.remove();
    addAgentMessage(payload?.answer ?? payload);
    setConnection("online", "Connected");
  } catch (error) {
    typingMessage.remove();
    addAgentMessage(
      {
        message:
          error instanceof TypeError
            ? "I could not reach the agent service. Check the endpoint under Connection."
            : error.message,
      },
      true,
    );
    setConnection("error", "Connection issue");
  } finally {
    setLoading(false);
  }
}

function setLoading(isLoading) {
  sendButton.disabled = isLoading;
  messageInput.disabled = isLoading;
}

function setConnection(state, label) {
  connectionStatus.textContent = label;
  statusDot.className = `status-dot ${state === "online" ? "online" : state === "error" ? "error" : ""}`;
}

function addUserMessage(message) {
  const wrapper = document.createElement("div");
  wrapper.className = "user-message";
  wrapper.innerHTML = `
    <div class="message-content">
      <span class="message-meta">You <time>now</time></span>
      <div class="message-bubble user-bubble"></div>
    </div>`;
  wrapper.querySelector(".user-bubble").textContent = message;
  conversation.append(wrapper);
  scrollConversation();
}

function addAgentMessage(answer, isError = false) {
  const wrapper = document.createElement("div");
  wrapper.className = "welcome-message";
  const message = extractMessage(answer);
  const result = extractResult(answer, message);
  wrapper.innerHTML = `
    <div class="agent-avatar">N</div>
    <div class="message-content">
      <span class="message-meta">Northstar Agent <time>now</time></span>
      <div class="message-bubble agent-bubble${isError ? " error-bubble" : ""}">
        <p></p>
        ${result ? `<div class="result-block"></div>` : ""}
      </div>
    </div>`;
  wrapper.querySelector(".message-bubble p").textContent = message;
  if (result) wrapper.querySelector(".result-block").textContent = result;
  conversation.append(wrapper);
  scrollConversation();
}

function extractMessage(answer) {
  if (typeof answer === "string") return answer;
  if (answer?.message) return answer.message;
  if (answer?.answer && typeof answer.answer === "string") return answer.answer;
  if (answer?.status === "ok")
    return "Your request was completed. Here are the details:";
  return "Here is what I found for your request:";
}

function extractResult(answer, message) {
  if (!answer || typeof answer !== "object") return "";
  const visible = { ...answer };
  delete visible.message;
  delete visible.answer;
  delete visible.tools;
  if (Object.keys(visible).length === 0) return "";
  return JSON.stringify(visible, null, 2);
}

function addTypingMessage() {
  const wrapper = document.createElement("div");
  wrapper.className = "welcome-message";
  wrapper.innerHTML = `
    <div class="agent-avatar">N</div>
    <div class="message-content">
      <span class="message-meta">Northstar Agent <time>working</time></span>
      <div class="message-bubble agent-bubble typing"><span></span><span></span><span></span></div>
    </div>`;
  conversation.append(wrapper);
  scrollConversation();
  return wrapper;
}

function scrollConversation() {
  conversation.scrollTop = conversation.scrollHeight;
}
