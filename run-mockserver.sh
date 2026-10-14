#!/usr/bin/env bash
set -e
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"
cd "$DIR"
echo "==================================================="
echo "[MockServer] Starting Tum.Trust.Security.MockServer ..."
echo "[MockServer] Root: $DIR"
echo "[MockServer] Listening on: http://localhost:5199"
echo "==================================================="
dotnet run --project "$DIR/MockServer/Tum.Trust.MockServer.csproj"
