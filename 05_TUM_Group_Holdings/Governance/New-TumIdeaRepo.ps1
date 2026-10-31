<#
.SYNOPSIS
    Automated Idea Repository Generator & Workspace Provisioner for TUM Group Holdings.
.DESCRIPTION
    Creates a new isolated Git repository for innovative ideas, scaffolds standard TUM architecture docs,
    optionally creates the GitHub remote repository via 'gh' CLI, registers an 'Idea' ticket on Jira via 'orca',
    and dynamically binds the repository into the central 'TUM_GROUP_ECOSYSTEM.code-workspace'.
.EXAMPLE
    .\New-TumIdeaRepo.ps1 -Name "zk-agent-identity" -Description "Zero-Knowledge AI Agent Trust Proofs"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Name,

    [Parameter(Mandatory = $false, Position = 1)]
    [string]$Description = "Innovative R&D Idea & Moonshot Project",

    [Parameter(Mandatory = $false)]
    [ValidateSet("private", "public")]
    [string]$Visibility = "private",

    [Parameter(Mandatory = $false)]
    [switch]$CreateRemote = $true,

    [Parameter(Mandatory = $false)]
    [switch]$CreateJira = $true
)

$ErrorActionPreference = "Stop"

# Normalize repo name
$cleanName = $Name.ToLower().Trim() -replace "[^a-z0-9\-]", "-"
if (-not $cleanName.StartsWith("idea-")) {
    $repoFolderName = "idea-$cleanName"
} else {
    $repoFolderName = $cleanName
}

$baseDir = "D:\TUM"
$ideasDir = Join-Path $baseDir "ideas"
if (-not (Test-Path $ideasDir)) {
    New-Item -ItemType Directory -Path $ideasDir -Force | Out-Null
}

$repoPath = Join-Path $ideasDir $repoFolderName
if (Test-Path $repoPath) {
    Write-Error "Repository folder already exists at: $repoPath"
    return
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "🚀 TUM GROUP IDEA REPO INCUBATOR: $repoFolderName" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "[1/5] Creating directory at $repoPath..." -ForegroundColor Green
New-Item -ItemType Directory -Path $repoPath -Force | Out-Null

# Scaffold standard TUM files
Write-Host "[2/5] Scaffolding standard architecture files..." -ForegroundColor Green
$readmeContent = @"
# 💡 $repoFolderName — $Description
## โครงการนวัตกรรมและไอเดียใหม่ (TUM Innovation Incubator)
**เครือข่าย:** TUM Group Holdings Co., Ltd.  
**สภานวัตกรรม:** TUM Innovation & Research Council  
**สถานะ:** Incubating / Exploratory Idea 💡  
**วันที่ริเริ่ม:** $(Get-Date -Format "yyyy-MM-dd")

---

## 1. วัตถุประสงค์และที่มาของไอเดีย (Executive Brief)
$Description

---

## 2. ขอบเขตการทดลอง (Proof-of-Concept Scope)
- [ ] สรุปความต้องการและปัญหาของลูกค้า/ตลาด (Problem-Solution Fit)
- [ ] พัฒนาชุดโค้ดต้นแบบขั้นต่ำ (Minimum Viable Prototype - MVP)
- [ ] ทดสอบประสิทธิภาพและความปลอดภัยเบื้องต้น (Benchmark & Safety)
- [ ] นำเสนอต่อสภานวัตกรรม (TUM Innovation Council) เพื่อพิจารณาบรรจุเข้าเสาหลักบริษัท

---

## 3. ความเชื่อมโยงกับเสาหลักธุรกิจ (Target Pillar Alignment)
- [ ] 01_TUM_Trust_Security (Sovereign Identity & Kiosks)
- [ ] 02_TUM_Socs (Cyber Defense & SIEM)
- [ ] 03_TUM_Quantum (Post-Quantum Cryptography)
- [ ] 04_Central_Director (Executive Dashboards & HUD)
"@
Set-Content -Path (Join-Path $repoPath "README.md") -Value $readmeContent -Encoding UTF8

$gitignoreContent = @"
bin/
obj/
*.user
*.suo
.vs/
.idea/
.DS_Store
*.log
"@
Set-Content -Path (Join-Path $repoPath ".gitignore") -Value $gitignoreContent -Encoding UTF8

# Initialize Git
Write-Host "[3/5] Initializing independent Git repository..." -ForegroundColor Green
Push-Location $repoPath
try {
    git init -b main | Out-Null
    git add . | Out-Null
    git commit -m "feat(incubator): initialize $repoFolderName — $Description" | Out-Null
    Write-Host "  ✓ Git initialized and initial commit created." -ForegroundColor Gray

    # GitHub Remote Creation via gh CLI
    if ($CreateRemote) {
        $ghInstalled = Get-Command "gh" -ErrorAction SilentlyContinue
        if ($ghInstalled) {
            Write-Host "  → Creating GitHub remote repository via gh CLI ($Visibility)..." -ForegroundColor Yellow
            $ghOutput = & gh repo create "PasitTum/$repoFolderName" "--$Visibility" --source=. --remote=origin --push 2>&1
            Write-Host "  ✓ GitHub remote created: https://github.com/PasitTum/$repoFolderName" -ForegroundColor Gray
        } else {
            Write-Host "  ! 'gh' CLI not found. Skipping remote GitHub creation." -ForegroundColor DarkYellow
        }
    }
} finally {
    Pop-Location
}

# Update TUM_GROUP_ECOSYSTEM.code-workspace
Write-Host "[4/5] Binding into TUM_GROUP_ECOSYSTEM.code-workspace..." -ForegroundColor Green
$workspaceFilePath = Join-Path $baseDir "TUM_GROUP_ECOSYSTEM.code-workspace"
if (Test-Path $workspaceFilePath) {
    try {
        $workspaceJson = Get-Content -Raw $workspaceFilePath | ConvertFrom-Json
        $relativePath = "ideas/$repoFolderName"
        
        $alreadyExists = $workspaceJson.folders | Where-Object { $_.path -eq $relativePath }
        if (-not $alreadyExists) {
            $newFolder = [PSCustomObject]@{
                name = "💡 Idea: $repoFolderName"
                path = $relativePath
            }
            $workspaceJson.folders += $newFolder
            $updatedJson = $workspaceJson | ConvertTo-Json -Depth 10
            Set-Content -Path $workspaceFilePath -Value $updatedJson -Encoding UTF8
            Write-Host "  ✓ Dynamically attached '$repoFolderName' into workspace file!" -ForegroundColor Gray
        }
    } catch {
        Write-Warning "Could not update workspace file: $_"
    }
}

# Create Jira Issue in 'Idea' status via Orca
if ($CreateJira) {
    Write-Host "[5/5] Registering idea on Jira Cloud via Orca CLI..." -ForegroundColor Green
    try {
        $jiraOutput = & orca jira create --project SCRUM --type 10004 --title "[Idea] $repoFolderName: $Description" --body "New innovative idea incubated in independent repository under 'ideas/$repoFolderName'. Proposed to TUM Innovation Council." --site z-3mP_i47N7xAgZ9psDZoJll 2>&1
        Write-Host "  ✓ Jira ticket created via Orca: $jiraOutput" -ForegroundColor Gray
    } catch {
        Write-Warning "Could not create Jira ticket via Orca: $_"
    }
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "🎉 IDEA REPO CREATED SUCCESSFULLY!" -ForegroundColor Cyan
Write-Host "   Path: $repoPath" -ForegroundColor White
Write-Host "   Open 'D:\TUM\TUM_GROUP_ECOSYSTEM.code-workspace' to view in editor." -ForegroundColor White
Write-Host "============================================================" -ForegroundColor Cyan
