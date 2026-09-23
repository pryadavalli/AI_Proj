# ClientWebApp

Static web client for the AI banking agent.

## Start everything

Double-click `start-all.bat`, or run this from PowerShell:

```powershell
Start-Process .\ClientWebApp\start-all.bat
```

This opens separate terminals for BankingService, AI_Agent, and the client web server, then opens the client at `http://localhost:8080`.

Ollama must already be installed and running with the configured model.

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

From the repository root, open `ClientWebApp/index.html` in your default browser:

```powershell
Start-Process (Resolve-Path .\ClientWebApp\index.html)
```

The client calls the AI agent at `http://localhost:5000` by default. The endpoint can be changed under **Connection** in the client.
