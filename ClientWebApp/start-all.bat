@echo off
setlocal

set "CLIENT_DIR=%~dp0"
for %%I in ("%CLIENT_DIR%..") do set "ROOT_DIR=%%~fI"

start "BankingService" powershell -NoExit -ExecutionPolicy Bypass -Command "Set-Location -LiteralPath '%ROOT_DIR%\BankingService'; dotnet run --urls http://localhost:5078"
start "AI_Agent" powershell -NoExit -ExecutionPolicy Bypass -Command "Set-Location -LiteralPath '%ROOT_DIR%\AI_Agent'; dotnet run --urls http://localhost:5000"
start "ClientWebApp" powershell -NoExit -ExecutionPolicy Bypass -Command "Set-Location -LiteralPath '%CLIENT_DIR%'; npm run dev -- --host localhost --port 8080"

for /l %%I in (1,1,15) do (
	powershell -NoProfile -Command "try { Invoke-WebRequest -Uri 'http://localhost:8080' -UseBasicParsing -TimeoutSec 1 | Out-Null; exit 0 } catch { exit 1 }" >nul 2>&1
	if not errorlevel 1 goto client_ready
	timeout /t 1 /nobreak >nul
)

:client_ready
start "" http://localhost:8080

endlocal
