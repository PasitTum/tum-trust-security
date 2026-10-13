@echo off
setlocal
cd /d "%~dp0"
echo ===================================================
echo [MockServer] Starting Netka.Identity.MockServer ...
echo [MockServer] Root: %CD%
echo [MockServer] Listening on: http://localhost:5199
echo ===================================================
dotnet run --project "MockServer/Netka.Identity.MockServer.csproj"
