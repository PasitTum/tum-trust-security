<#
.SYNOPSIS
    Installs or updates the Claude Code Global Skill (integrate-netka-identity) to ~/.claude/skills/
    so that /integrate-netka-identity can be invoked from any project on this machine.
#>

$ErrorActionPreference = "Stop"

$SourceSkillDir = Join-Path $PSScriptRoot ".claude\skills\integrate-netka-identity"
$SourceSkillFile = Join-Path $SourceSkillDir "SKILL.md"

if (-not (Test-Path $SourceSkillFile)) {
    Write-Error "Source skill file not found at: $SourceSkillFile"
    exit 1
}

$GlobalClaudeSkillDir = Join-Path $HOME ".claude\skills\integrate-netka-identity"
$GlobalClaudeSkillFile = Join-Path $GlobalClaudeSkillDir "SKILL.md"

if (-not (Test-Path $GlobalClaudeSkillDir)) {
    New-Item -ItemType Directory -Force -Path $GlobalClaudeSkillDir | Out-Null
}

Copy-Item -Path $SourceSkillFile -Destination $GlobalClaudeSkillFile -Force

Write-Host "[OK] Global Skill installed successfully!" -ForegroundColor Green
Write-Host "  Source (Git):   $SourceSkillFile"
Write-Host "  Global Target:  $GlobalClaudeSkillFile"
Write-Host ""
Write-Host "Usage in any host project:" -ForegroundColor Cyan
Write-Host "  1. Open terminal in target project and run: claude"
Write-Host "  2. Run: /integrate-netka-identity"
