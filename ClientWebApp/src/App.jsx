import { useEffect, useRef, useState } from "react";

const defaultApiUrl = "http://localhost:5000";
const suggestions = [
  "List all accounts in a table form",
  "Which customer has more transactions? Check all accounts.",
  "I need to withdraw money",
];

function App() {
  const [apiUrl, setApiUrl] = useState(
    () => localStorage.getItem("aiAgentApiUrl") || defaultApiUrl,
  );
  const [draftUrl, setDraftUrl] = useState(apiUrl);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [status, setStatus] = useState({ state: "", label: "Ready to connect" });
  const [messages, setMessages] = useState([
    {
      id: "welcome",
      role: "agent",
      message:
        "Good morning. I can help with accounts, balances, transactions, and withdrawals.\n\nWhat would you like to take care of?",
    },
  ]);
  const [input, setInput] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const conversationRef = useRef(null);
  const inputRef = useRef(null);

  useEffect(() => {
    conversationRef.current?.scrollTo({
      top: conversationRef.current.scrollHeight,
      behavior: "smooth",
    });
  }, [messages, isLoading]);

  function saveConnection(event) {
    event.preventDefault();
    const nextUrl = draftUrl.trim().replace(/\/$/, "") || defaultApiUrl;
    setApiUrl(nextUrl);
    setDraftUrl(nextUrl);
    localStorage.setItem("aiAgentApiUrl", nextUrl);
    setStatus({ state: "", label: "Endpoint saved" });
    setSettingsOpen(false);
  }

  function chooseSuggestion(prompt) {
    setInput(prompt);
    inputRef.current?.focus();
  }

  async function sendMessage(event) {
    event?.preventDefault();
    const message = input.trim();
    if (!message || isLoading) return;

    setMessages((current) => [
      ...current,
      { id: crypto.randomUUID(), role: "user", message },
    ]);
    setInput("");
    setIsLoading(true);

    try {
      const response = await fetch(`${apiUrl}/api/agent`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message }),
      });
      const payload = await response.json().catch(() => null);
      if (!response.ok) {
        throw new Error(payload?.message || `Agent service returned ${response.status}.`);
      }

      setMessages((current) => [
        ...current,
        { id: crypto.randomUUID(), role: "agent", answer: payload?.answer ?? payload },
      ]);
      setStatus({ state: "online", label: "Connected" });
    } catch (error) {
      setMessages((current) => [
        ...current,
        {
          id: crypto.randomUUID(),
          role: "agent",
          isError: true,
          message:
            error instanceof TypeError
              ? "I could not reach the agent service. Check the endpoint under Connection."
              : error.message,
        },
      ]);
      setStatus({ state: "error", label: "Connection issue" });
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-mark" aria-hidden="true">N</div>
        <div className="brand-copy">
          <span className="eyebrow">Northstar</span>
          <strong>Banking desk</strong>
        </div>
        <div className="sidebar-rule" />
        <div className="service-status">
          <span className={`status-dot ${status.state}`} />
          <div>
            <span className="status-label">Agent service</span>
            <strong>{status.label}</strong>
          </div>
        </div>
        <div className="sidebar-bottom">
          <div className="secure-note">
            <span className="lock-icon" aria-hidden="true">&#128274;</span>
            <span>Private banking workspace</span>
          </div>
          <p className="version">AI_Agent client / React</p>
        </div>
      </aside>

      <main className="chat-panel">
        <header className="topbar">
          <div>
            <p className="eyebrow">Personal banking</p>
            <h1>How can we move<br /><em>your money forward?</em></h1>
          </div>
          <button
            className="settings-button"
            type="button"
            aria-expanded={settingsOpen}
            aria-controls="settingsPanel"
            onClick={() => setSettingsOpen((open) => !open)}
          >
            <span className="settings-icon" aria-hidden="true">&#9881;</span>
            <span>Connection</span>
          </button>
        </header>

        {settingsOpen && (
          <section className="settings-panel" id="settingsPanel">
            <form onSubmit={saveConnection}>
              <label htmlFor="apiUrl">AI_Agent API endpoint</label>
              <div className="endpoint-row">
                <input
                  id="apiUrl"
                  type="url"
                  value={draftUrl}
                  onChange={(event) => setDraftUrl(event.target.value)}
                  autoComplete="url"
                />
                <button className="small-button" type="submit">Save</button>
              </div>
            </form>
            <p>Use the URL where the AI_Agent service is running.</p>
          </section>
        )}

        <section className="conversation" ref={conversationRef} aria-live="polite" aria-label="Banking conversation">
          {messages.map((item) => (
            <Message key={item.id} item={item} />
          ))}
          {isLoading && <TypingMessage />}
        </section>

        <section className="composer-area">
          <div className="suggestions" aria-label="Suggested banking requests">
            {suggestions.map((suggestion) => (
              <button key={suggestion} type="button" className="suggestion" onClick={() => chooseSuggestion(suggestion)}>
                {suggestion} <span aria-hidden="true">&#8599;</span>
              </button>
            ))}
          </div>
          <form className="composer" onSubmit={sendMessage}>
            <label className="sr-only" htmlFor="messageInput">Message the banking agent</label>
            <textarea
              ref={inputRef}
              id="messageInput"
              rows="1"
              value={input}
              placeholder="Ask about your banking needs..."
              disabled={isLoading}
              onChange={(event) => setInput(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter" && !event.shiftKey) {
                  event.preventDefault();
                  sendMessage(event);
                }
              }}
            />
            <button className="send-button" type="submit" aria-label="Send message" disabled={isLoading}>
              <span aria-hidden="true">&#8593;</span>
            </button>
          </form>
          <p className="composer-hint">Press Enter to send <span>·</span> Shift + Enter for a new line</p>
        </section>
      </main>
    </div>
  );
}

function Message({ item }) {
  if (item.role === "user") {
    return (
      <div className="user-message">
        <div className="message-content">
          <span className="message-meta">You <time>now</time></span>
          <div className="message-bubble user-bubble">{item.message}</div>
        </div>
      </div>
    );
  }

  const message = item.message || extractMessage(item.answer);
  const data = item.answer?.data;
  const result = extractResult(item.answer);
  return (
    <div className="welcome-message">
      <div className="agent-avatar">N</div>
      <div className="message-content">
        <span className="message-meta">Northstar Agent <time>now</time></span>
        <div className={`message-bubble agent-bubble${item.isError ? " error-bubble" : ""}`}>
          {createDataRows(data) ? <DataTable data={data} /> : <p>{message}</p>}
          {result && <div className="result-block">{result}</div>}
        </div>
      </div>
    </div>
  );
}

function DataTable({ data }) {
  const headers = [...new Set(data.flatMap((row) => Object.keys(row)))];
  return (
    <div className="table-wrapper">
      <table className="response-table">
        <thead><tr>{headers.map((header) => <th key={header}>{header}</th>)}</tr></thead>
        <tbody>{data.map((row, index) => (
          <tr key={row.id || index}>{headers.map((header) => <td key={header}>{String(row[header] ?? "")}</td>)}</tr>
        ))}</tbody>
      </table>
    </div>
  );
}

function createDataRows(data) {
  return Array.isArray(data) && data.length > 0 && data.every((row) => row && typeof row === "object" && !Array.isArray(row));
}

function extractMessage(answer) {
  if (typeof answer === "string") return answer;
  if (answer?.message) return answer.message;
  if (answer?.answer && typeof answer.answer === "string") return answer.answer;
  if (answer?.status === "ok") return "Your request was completed. Here are the details:";
  return "Here is what I found for your request:";
}

function extractResult(answer) {
  if (!answer || typeof answer !== "object") return "";
  const visible = { ...answer };
  delete visible.message;
  delete visible.answer;
  delete visible.tools;
  delete visible.data;
  return Object.keys(visible).length ? JSON.stringify(visible, null, 2) : "";
}

function TypingMessage() {
  return (
    <div className="welcome-message">
      <div className="agent-avatar">N</div>
      <div className="message-content">
        <span className="message-meta">Northstar Agent <time>working</time></span>
        <div className="message-bubble agent-bubble typing"><span /><span /><span /></div>
      </div>
    </div>
  );
}

export default App;
