@echo off
setlocal

set "CLIENT_DIR=%~dp0"
for %%I in ("%CLIENT_DIR%..") do set "ROOT_DIR=%%~fI"

start "BankingService" powershell -NoExit -ExecutionPolicy Bypass -Command "Set-Location -LiteralPath '%ROOT_DIR%\BankingService'; dotnet run --urls http://localhost:5078"
start "AI_Agent" powershell -NoExit -ExecutionPolicy Bypass -Command "Set-Location -LiteralPath '%ROOT_DIR%\AI_Agent'; dotnet run --urls http://localhost:5000"
start "ClientWebApp" powershell -NoExit -ExecutionPolicy Bypass -Command "python -m http.server 8080 --directory '%CLIENT_DIR%'"

timeout /t 2 /nobreak >nul
start "" http://localhost:8080

endlocal
