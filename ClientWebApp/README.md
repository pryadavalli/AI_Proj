# ClientWebApp

React/Vite web client for the AI banking agent.

## Start everything

Double-click `start-all.bat`, or run this from PowerShell:

```powershell
Start-Process .\ClientWebApp\start-all.bat
```

Run that command from the repository root. If your terminal is already inside `ClientWebApp`, use `Start-Process .\start-all.bat` instead.

This opens separate terminals for BankingService, AI_Agent, and the Vite client server, then opens the client at `http://localhost:8080`.

Ollama must already be installed and running with the configured model.

Install the client dependencies once before using the batch file:

```powershell
Set-Location .\ClientWebApp
npm install
```

## Run the services

From the repository root, open separate PowerShell terminals and run:

### BankingService

```powershell
dotnet run --project .\BankingService\BankingService.csproj --urls http://localhost:5078
```

### AI_Agent

```powershell
dotnet run --project .\AI_Agent\AI_Agent.csproj --urls http://localhost:5000
```

The AI agent calls the BankingService at `http://localhost:5078`.

## Launch the client

From the repository root, start the Vite development server:

```powershell
Set-Location .\ClientWebApp
npm run dev -- --host localhost --port 8080
```

The client calls the AI agent at `http://localhost:5000` by default. The endpoint can be changed under **Connection** in the client.
