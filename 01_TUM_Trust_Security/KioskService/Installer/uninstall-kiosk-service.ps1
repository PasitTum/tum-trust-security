<#
.SYNOPSIS
    Uninstalls the TUM Trust Security Kiosk HAL Background Service.
#>

[CmdletBinding()]
param (
    [string]$ServiceName = "TumTrustKioskService"
)

Write-Host "Stopping and removing $ServiceName..." -ForegroundColor Yellow
Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
sc.exe delete $ServiceName
Write-Host "Service $ServiceName removed." -ForegroundColor Green
