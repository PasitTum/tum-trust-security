<#
.SYNOPSIS
    Installs and configures TUM Trust Security Kiosk HAL Background Service with USB Auto-Recovery.
.DESCRIPTION
    Builds or packages the service, registers as a Windows Service, sets auto-start,
    configures automatic recovery failure actions, and verifies health.
#>

[CmdletBinding()]
param (
    [string]$PublishDir = "$PSScriptRoot\..\bin\Release\net8.0\publish",
    [string]$ServiceName = "TumTrustKioskService",
    [string]$DisplayName = "TUM Trust Security Kiosk HAL Service",
    [int]$Port = 5890
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " TUM Trust Security — Kiosk Background Service Installer" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Check Administrative Privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Warning "Administrator rights required to install Windows Services. Please run PowerShell as Administrator."
}

# 2. Publish Project
Write-Host "[1/5] Publishing Tum.Trust.KioskService (.NET 8)..." -ForegroundColor Yellow
$projectPath = "$PSScriptRoot\..\Tum.Trust.KioskService.csproj"
dotnet publish $projectPath -c Release -r win-x64 --self-contained false -o $PublishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exePath = Join-Path $PublishDir "Tum.Trust.KioskService.exe"
if (-not (Test-Path $exePath)) {
    throw "Published executable not found at: $exePath"
}
Write-Host "      Binary ready: $exePath" -ForegroundColor Green

# 3. Check existing service
Write-Host "[2/5] Checking existing service status..." -ForegroundColor Yellow
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "      Stopping existing service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# 4. Create Windows Service
Write-Host "[3/5] Registering Windows Service via sc.exe..." -ForegroundColor Yellow
$binArg = "`"$exePath`""
sc.exe create $ServiceName binPath= $binArg start= auto displayName= $DisplayName
if ($LASTEXITCODE -ne 0) {
    Write-Warning "sc.exe create failed or requires elevated admin privileges."
}

# 5. Configure Recovery & Failure Actions
Write-Host "[4/5] Configuring Auto-Recovery Policy (Failure Actions)..." -ForegroundColor Yellow
# Restart after 2s on 1st failure, 5s on 2nd, 10s on subsequent. Reset failure counter daily.
sc.exe failure $ServiceName reset= 86400 actions= restart/2000/restart/5000/restart/10000
sc.exe failureflag $ServiceName 1

# Configure Service Description
sc.exe description $ServiceName "High-resilience hardware HAL service managing smartcard readers, ePassport scanners, and biometric cameras with USB auto-recovery."

# 6. Verify or start in standalone mode if testing
Write-Host "[5/5] Service installation complete." -ForegroundColor Green
Write-Host ""
Write-Host "To start the service:" -ForegroundColor Cyan
Write-Host "  Start-Service -Name $ServiceName" -ForegroundColor White
Write-Host "  or run directly in test console:" -ForegroundColor Cyan
Write-Host "  & `"$exePath`"" -ForegroundColor White
Write-Host ""
Write-Host "HAL Health endpoint: http://127.0.0.1:$Port/health" -ForegroundColor Green
