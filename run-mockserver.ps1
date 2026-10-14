$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot
Set-Location $repoRoot
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "[MockServer] Starting Tum.Trust.Security.MockServer ..." -ForegroundColor Cyan
Write-Host "[MockServer] Root: $repoRoot" -ForegroundColor Gray
Write-Host "[MockServer] Listening on: http://localhost:5199" -ForegroundColor Green
Write-Host "===================================================" -ForegroundColor Cyan
dotnet run --project "$repoRoot/MockServer/Tum.Trust.MockServer.csproj"
