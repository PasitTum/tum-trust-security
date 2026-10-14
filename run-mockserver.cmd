@echo off
setlocal
cd /d "%~dp0"
echo ===================================================
echo [MockServer] Starting Tum.Trust.Security.MockServer ...
echo [MockServer] Root: %CD%
echo [MockServer] Listening on: http://localhost:5199
echo ===================================================
dotnet run --project "MockServer/Tum.Trust.MockServer.csproj"
