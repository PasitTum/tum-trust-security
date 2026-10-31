<#
.SYNOPSIS
    Installs or updates skills to ~/.claude/skills/
    and parent workspace .claude/skills/ (if present).
.PARAMETER All
    If specified, syncs all skills found in .claude/skills/ rather than only integrate-tum-trust.
#>
param(
    [switch]$All
)

$ErrorActionPreference = "Stop"

$SourceSkillsDir = Join-Path $PSScriptRoot ".claude\skills"

if (-not (Test-Path $SourceSkillsDir)) {
    Write-Error "Source skills directory not found at: $SourceSkillsDir"
    exit 1
}

$GlobalClaudeSkillsDir = Join-Path $HOME ".claude\skills"
$ParentClaudeSkillsRoot = Join-Path (Split-Path $PSScriptRoot -Parent) ".claude\skills"

$SkillFolders = if ($All) {
    Get-ChildItem -Path $SourceSkillsDir -Directory
} else {
    Get-Item (Join-Path $SourceSkillsDir "integrate-tum-trust")
}

foreach ($folder in $SkillFolders) {
    $skillName = $folder.Name
    $targetGlobal = Join-Path $GlobalClaudeSkillsDir $skillName
    
    if (-not (Test-Path $targetGlobal)) {
        New-Item -ItemType Directory -Force -Path $targetGlobal | Out-Null
    }
    
    Copy-Item -Path (Join-Path $folder.FullName "*") -Destination $targetGlobal -Recurse -Force
    Write-Host "[OK] Synced $skillName -> $targetGlobal" -ForegroundColor Green
    
    if (Test-Path $ParentClaudeSkillsRoot) {
        $parentTarget = Join-Path $ParentClaudeSkillsRoot $skillName
        if (-not (Test-Path $parentTarget)) {
            New-Item -ItemType Directory -Force -Path $parentTarget | Out-Null
        }
        Copy-Item -Path (Join-Path $folder.FullName "*") -Destination $parentTarget -Recurse -Force
        Write-Host "     Workspace Root: $parentTarget" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Usage in any host project:" -ForegroundColor Cyan
Write-Host "  1. Run: claude (or use Antigravity / Gemini CLI)"
Write-Host "  2. Skills are automatically discovered and ready to use."
