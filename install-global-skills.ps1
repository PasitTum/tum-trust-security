<#
.SYNOPSIS
    Installs or updates the Claude Code Global Skill (integrate-netka-identity) to ~/.claude/skills/
    and parent workspace .claude/skills/ (if present) so that /integrate-netka-identity is always up to date.
#>

$ErrorActionPreference = "Stop"

$SourceSkillDir = Join-Path $PSScriptRoot ".claude\skills\integrate-netka-identity"
$SourceSkillFile = Join-Path $SourceSkillDir "SKILL.md"

if (-not (Test-Path $SourceSkillFile)) {
    Write-Error "Source skill file not found at: $SourceSkillFile"
    exit 1
}

# 1. Sync to Global ~/.claude/skills/integrate-netka-identity/SKILL.md
$GlobalClaudeSkillDir = Join-Path $HOME ".claude\skills\integrate-netka-identity"
$GlobalClaudeSkillFile = Join-Path $GlobalClaudeSkillDir "SKILL.md"

if (-not (Test-Path $GlobalClaudeSkillDir)) {
    New-Item -ItemType Directory -Force -Path $GlobalClaudeSkillDir | Out-Null
}
Copy-Item -Path $SourceSkillFile -Destination $GlobalClaudeSkillFile -Force

Write-Host "[OK] Global Skill installed successfully!" -ForegroundColor Green
Write-Host "  Source (Git):   $SourceSkillFile"
Write-Host "  Global Target:  $GlobalClaudeSkillFile"

# 2. Sync to Parent Workspace .claude/skills/integrate-netka-identity/SKILL.md (if parent .claude/skills exists)
$ParentClaudeSkillsRoot = Join-Path (Split-Path $PSScriptRoot -Parent) ".claude\skills"
if (Test-Path $ParentClaudeSkillsRoot) {
    $ParentTargetDir = Join-Path $ParentClaudeSkillsRoot "integrate-netka-identity"
    $ParentTargetFile = Join-Path $ParentTargetDir "SKILL.md"
    if (-not (Test-Path $ParentTargetDir)) {
        New-Item -ItemType Directory -Force -Path $ParentTargetDir | Out-Null
    }
    Copy-Item -Path $SourceSkillFile -Destination $ParentTargetFile -Force
    Write-Host "  Workspace Root: $ParentTargetFile" -ForegroundColor Green
}

Write-Host ""
Write-Host "Usage in any host project:" -ForegroundColor Cyan
Write-Host "  1. Open terminal in target project and run: claude"
Write-Host "  2. Run: /integrate-netka-identity"
