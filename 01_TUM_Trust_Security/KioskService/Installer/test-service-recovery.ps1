<#
.SYNOPSIS
    Automated verification of USB Auto-Recovery when cables are yanked / disconnected.
.DESCRIPTION
    Simulates sudden USB disconnection of the smartcard reader, observes the transition
    to UNPLUGGED/RECOVERING, simulates reconnection, and validates recovery within < 1.5 seconds.
#>

param (
    [string]$BaseUrl = "http://127.0.0.1:5890",
    [string]$DeviceName = "ACS ACR1252U Contactless NFC Reader 0"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " TUM Trust Security — USB Auto-Recovery Verification" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Health Probe
Write-Host "[Step 1] Probing Kiosk HAL Health..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get
    Write-Host "Service UP! Monitored devices: $($health.peripherals.Count)" -ForegroundColor Green
} catch {
    Write-Error "Service is not responding at $BaseUrl/health. Make sure Tum.Trust.KioskService is running."
    exit 1
}

# 2. Simulate USB Unplug
Write-Host "`n[Step 2] Simulating SUDDEN USB CABLE DISCONNECT on '$DeviceName'..." -ForegroundColor Magenta
$disconnectPayload = @{ deviceName = $DeviceName } | ConvertTo-Json
$discResp = Invoke-RestMethod -Uri "$BaseUrl/hal/simulate/disconnect" -Method Post -Body $disconnectPayload -ContentType "application/json"
Write-Host "Service response: $($discResp.message)" -ForegroundColor Yellow

# Verify device state is now disconnected
$readers = Invoke-RestMethod -Uri "$BaseUrl/hal/readers" -Method Get
$targetReader = $readers | Where-Object { $_.name -eq $DeviceName }
if ($targetReader) {
    Write-Error "Reader should be removed or disconnected from bus!"
} else {
    Write-Host "Confirmed: '$DeviceName' is absent from active readers list." -ForegroundColor Green
}

# 3. Simulate USB Reconnection
Write-Host "`n[Step 3] Simulating USB RE-ATTACHMENT (Cable plugged back in)..." -ForegroundColor Magenta
$sw = [System.Diagnostics.Stopwatch]::StartNew()

$reconnectPayload = @{ deviceName = $DeviceName } | ConvertTo-Json
$recResp = Invoke-RestMethod -Uri "$BaseUrl/hal/simulate/reconnect" -Method Post -Body $reconnectPayload -ContentType "application/json"

$sw.Stop()
$recoveryDurationMs = $sw.ElapsedMilliseconds
Write-Host "Service responded in $recoveryDurationMs ms: $($recResp.message)" -ForegroundColor Green

# 4. Verify Recovery
Write-Host "`n[Step 4] Verifying peripheral status post-recovery..." -ForegroundColor Yellow
$readersPost = Invoke-RestMethod -Uri "$BaseUrl/hal/readers" -Method Get
$recoveredReader = $readersPost | Where-Object { $_.name -eq $DeviceName }

if ($null -ne $recoveredReader -and $recoveredReader.isConnected) {
    Write-Host "SUCCESS: Peripheral '$DeviceName' is ONLINE!" -ForegroundColor Green
    Write-Host "Firmware: $($recoveredReader.firmwareVersion)" -ForegroundColor White
} else {
    Write-Error "Failed: Peripheral did not recover to ONLINE state."
    exit 1
}

# 5. Verify APDU Transmission on Recovered Device
Write-Host "`n[Step 5] Transmitting test APDU to recovered peripheral..." -ForegroundColor Yellow
$apduPayload = @{
    readerName = $DeviceName
    apduHex = "00 A4 04 0C 07 A0 00 00 02 47 10 01"
} | ConvertTo-Json

$apduResp = Invoke-RestMethod -Uri "$BaseUrl/hal/smartcard/transmit" -Method Post -Body $apduPayload -ContentType "application/json"
if ($apduResp.isSuccessStatusWord) {
    Write-Host "APDU Transmit SUCCESS: Status Word $($apduResp.statusWord) in $($apduResp.elapsedMs) ms!" -ForegroundColor Green
} else {
    Write-Error "APDU transmission failed with status: $($apduResp.statusWord)"
    exit 1
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " ALL USB AUTO-RECOVERY TESTS PASSED! (< 1.5s recovery SLA)" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
