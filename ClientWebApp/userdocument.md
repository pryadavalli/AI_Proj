# Northstar Banking Desk

## User Documentation

This document provides a user-oriented overview of the Northstar Banking Desk application, including setup, key features, and common workflows. It is intended to support day-to-day usage and operational understanding of the banking assistant experience.

---

## 1. Overview

Northstar Banking Desk is a web-based banking assistant that allows users to interact with an AI-powered agent to:

- view account information
- check balances
- review account activity and recent transactions
- request withdrawals and deposits
- create a new customer account
- ask natural-language banking questions in plain English

The interface is designed for a private banking workspace and is connected to a backend AI agent service that communicates with the banking API.

---

## 2. System Components

The application is made up of three main parts:

1. Client Web App
   - React/Vite front-end interface
   - Runs in the browser
   - Allows the user to type questions and receive banking responses

2. AI_Agent service
   - Receives the user message
   - Plans the banking action
   - Calls the appropriate backend banking tools
   - Returns a summarized answer to the user

3. BankingService
   - Hosts the banking data and operations
   - Stores account and transaction information
   - Supports account creation, balance lookup, withdrawals, and transaction history

---

## 3. Prerequisites

Before using the application, ensure the following are available:

- Microsoft .NET 8 SDK
- Node.js and npm
- Ollama installed and running
- The configured AI model available in Ollama
- The BankingService and AI_Agent services running locally

---

## 4. Starting the Application

### Option A: Start everything with the provided script

From the repository root, run:

```powershell
Start-Process .\ClientWebApp\start-all.bat
```

This script starts the BankingService, AI_Agent, and Vite client server and opens the interface in the browser.

### Option B: Start the services manually

From the repository root, open separate PowerShell terminals and run:

#### BankingService

```powershell
dotnet run --project .\BankingService\BankingService.csproj --urls http://localhost:5078
```

#### AI_Agent

```powershell
dotnet run --project .\AI_Agent\AI_Agent.csproj --urls http://localhost:5000
```

#### Client app

```powershell
Set-Location .\ClientWebApp
npm install
npm run dev -- --host localhost --port 8080
```

The client is usually available at:

```text
http://localhost:8080
```

---

## 5. Using the Interface

### Main screen

The interface is a chat-style banking assistant with:

- a left navigation area showing the service status
- a conversation pane for user and agent messages
- suggested prompts
- a message composer at the bottom of the screen
- a Connection button for endpoint configuration

### Suggested actions

The app includes prebuilt example prompts such as:

- List all accounts in a table form
- Which customer has more transactions? Check all accounts.
- I need to withdraw money

Users may click these suggestions or type their own request.

### Sending requests

Type a question or banking instruction into the input area and press Enter to send it.

Examples:

- Show all accounts
- What is the balance for account AC12345678?
- Withdraw $250 from account AC12345678
- Create an account for Jane Doe with an initial deposit of $1500
- Show recent transactions for account AC12345678

---

## 6. Core Banking Tasks

### View accounts

Users can ask the assistant to list customer accounts and display them in a table format.

Example request:

```text
List all accounts in a table form
```

### Check balance

The assistant can look up the current balance for a known account.

Example request:

```text
What is the balance for account AC12345678?
```

### Review transactions

Users can request recent transaction activity for a selected account.

Example request:

```text
Show me the last transactions for account AC12345678
```

### Withdraw funds

When a user provides the account and amount, the agent can process a withdrawal.

Example request:

```text
Withdraw $250 from account AC12345678
```

### Create a new account

The assistant can create a new banking account based on the customer name and opening deposit.

Example request:

```text
Create an account for Ada Lovelace with an initial deposit of $1000
```

---

## 7. Connection Settings

The client connects to the AI agent by default at:

```text
http://localhost:5000
```

If the AI agent is running on a different host or port, update the endpoint using the Connection button in the top-right corner of the client.

To change the endpoint:

1. Click Connection
2. Update the API URL
3. Click Save

The saved value is stored in local browser storage for future sessions.

---

## 8. Best Practices

- Ask specific questions with account identifiers or amounts when possible.
- Use natural language and clear business terms such as balance, withdrawal, deposit, or transactions.
- Review the returned data carefully before acting on a financial request.
- If the connection is unavailable, verify that the AI_Agent service is running and reachable.
- Keep Ollama running in the background while using the assistant.

---

## 9. Troubleshooting

### The app cannot connect to the agent

Check the following:

- the AI_Agent service is running
- the configured endpoint is correct
- the service is listening on the expected port
- the browser is not blocked by a local network issue

### The assistant returns an error

Possible causes include:

- invalid or missing account identifier
- no amount specified for a withdrawal or deposit request
- the banking service is not available
- the AI model is not ready in Ollama

### The client does not open

Verify the Vite process is running and the app was started with the correct host and port.

---

## 10. Security and Privacy Notes

This application is intended for a private banking workspace and should be used only in secure, approved environments. User interactions may involve personal and financial account data. Users should follow organizational security guidelines and avoid exposing sensitive information in unsecured or public environments.

---

## 11. Support and Maintenance

This application is designed for local development and demonstration use. For production use, additional security controls, authentication, monitoring, and audit logging should be added based on organizational standards.

---

## 12. Summary

The Northstar Banking Desk application provides a simple, natural-language banking experience through a browser-based assistant. It enables users to ask for financial information and complete common banking tasks without needing direct system or API knowledge.

This document is intended to support user onboarding, application walkthroughs, and operational review.
