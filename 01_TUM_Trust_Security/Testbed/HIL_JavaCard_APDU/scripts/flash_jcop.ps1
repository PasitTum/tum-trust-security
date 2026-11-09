<#
.SYNOPSIS
    PowerShell wrapper for flashing NXP JCOP Blank Smart Cards using GlobalPlatform.
#>

param (
    [string]$Reader = "ACS ACR1252U Contactless NFC Reader 0",
    [string]$CapFile = "$PSScriptRoot\..\applet\bin\ThaiIdAndPassportApplet.cap",
    [string]$GpKey = "404142434445464748494A4B4C4D4E4F",
    [switch]$DryRun = $false
)

Write-Host "Invoking TUM Trust Security JCOP Flasher..." -ForegroundColor Cyan
python "$PSScriptRoot\flash_jcop.py" --reader $Reader --cap $CapFile --key $GpKey
